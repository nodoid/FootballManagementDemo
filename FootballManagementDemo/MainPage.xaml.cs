using System.Text.Json;
using FootballManagementEngine;

namespace FootballManagementDemo;

public partial class MainPage : ContentPage
{
    private readonly FootballGameEngine _game;
    private readonly GameApi _api;
    private readonly SeasonEngine _season;
    private readonly List<TeamChoice> _teams = new();
    private readonly List<FormationChoice> _formations = new();

    public MainPage()
    {
        InitializeComponent();

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "football-management.db");
        _game = UkDatabase.Create(databasePath, loadExisting: true, autoSave: true, slot: "default");
        _season = new SeasonEngine(_game);
        _formations.AddRange(Enum.GetValues<Formation>().Select(f =>
            new FormationChoice(f, f.ToString())));

        FormationPicker.ItemsSource = _formations;

        if (_game.State.Fixtures.Count == 0)
        {
            _season.GenerateDomesticSeason();
            _season.GenerateFaCup();

            AddEuropeanTeams();
            _season.GenerateEuropeanFixtures();
            _game.SaveIfConfigured();
            SaveStatus.Text = "New career created and saved locally.";
        }
        else
        {
            SaveStatus.Text = "Saved career loaded from SQLite.";
        }

        LoadTeams();
        RefreshDashboard();
    }

    private void AddEuropeanTeams()
    {
        if (_game.State.Competitions.TryGetValue("UCL", out var ucl))
            ucl.TeamIds.AddRange(new[] { "ARS", "LIV", "MCI", "MUN", "CHE", "NEW" }.Where(id => !ucl.TeamIds.Contains(id)));
        if (_game.State.Competitions.TryGetValue("UEL", out var uel))
            uel.TeamIds.AddRange(new[] { "AVL", "TOT", "BHA", "WHU" }.Where(id => !uel.TeamIds.Contains(id)));
        if (_game.State.Competitions.TryGetValue("UECL", out var uecl))
            uecl.TeamIds.AddRange(new[] { "CRY", "FUL" }.Where(id => !uecl.TeamIds.Contains(id)));
    }

    private void LoadTeams()
    {
        var response = _api.Handle("GET", GameApi.TeamsPath);
        var payload = JsonSerializer.Deserialize<TeamListResponse>(response.Body, _game.JsonOptions);

        _teams.Clear();
        if (payload?.Teams is not null)
        {
            _teams.AddRange(payload.Teams.Select(t =>
                new TeamChoice(t.Id, $"{t.Name} ({t.ShortName}) — {t.LeagueId}")));
        }

        TeamPicker.ItemsSource = _teams;

        if (_game.State.PlayerTeamId is not null)
        {
            var selected = _teams.FirstOrDefault(t => t.Id == _game.State.PlayerTeamId);
            if (selected is not null)
                TeamPicker.SelectedItem = selected;
            RefreshFormationPicker();
        }
    }

    private void RefreshFormationPicker()
    {
        var team = _game.GetPlayerTeam();
        if (team is null) return;

        var selected = _formations.FirstOrDefault(f => f.Formation == team.Formation);
        if (selected is not null)
            FormationPicker.SelectedItem = selected;
        FormationStatus.Text = $"{team.ShortName}: {team.Formation}";
    }

    private void OnSelectTeamClicked(object sender, EventArgs e)
    {
        if (TeamPicker.SelectedItem is not TeamChoice choice)
        {
            SelectionStatus.Text = "Choose a team first.";
            return;
        }

        var response = _api.Handle("POST", GameApi.SelectTeamPath,
            JsonSerializer.Serialize(new { teamId = choice.Id }, _game.JsonOptions));

        ApiOutput.Text = PrettyJson(response.Body);

        if (response.StatusCode == 200)
        {
            var team = _game.GetPlayerTeam();
            SelectionStatus.Text = $"Now managing {team?.Name} ({team?.ShortName}).";
            RefreshFormationPicker();
            _game.SaveIfConfigured();
            SaveStatus.Text = "Career saved to SQLite.";
            RefreshDashboard();
        }
        else
        {
            SelectionStatus.Text = $"API error: HTTP {response.StatusCode}.";
        }
    }

    private void OnFormationChanged(object sender, EventArgs e)
    {
        if (TeamPicker.SelectedItem is not TeamChoice choice ||
            FormationPicker.SelectedItem is not FormationChoice formation)
            return;

        var response = _api.Handle("POST", GameApi.FormationPath,
            JsonSerializer.Serialize(new { teamId = choice.Id, formation = formation.Formation }, _game.JsonOptions));

        FormationStatus.Text = response.StatusCode == 200
            ? $"{choice.Id}: {formation.Formation} selected."
            : $"Formation error: HTTP {response.StatusCode}.";

        ApiOutput.Text = PrettyJson(response.Body);
        _game.SaveIfConfigured();
        SaveStatus.Text = "Formation change saved to SQLite.";
    }

    private void OnAdvanceWeekClicked(object sender, EventArgs e)
    {
        _season.ProcessWeek();
        _game.SaveIfConfigured();

        EngineStatus.Text = $"Advanced to {_game.State.CurrentDateUtc:dd MMM yyyy}. Weekly finance/injury processing completed.";
        SaveStatus.Text = "Career saved to SQLite.";
        RefreshDashboard();
    }

    private void OnSimulateClicked(object sender, EventArgs e)
    {
        var team = _game.GetPlayerTeam();
        if (team is null)
        {
            EngineStatus.Text = "Select a club before simulating a match.";
            return;
        }

        var fixture = _game.Fixtures(teamId: team.Id)
            .FirstOrDefault(f => !f.IsPlayed && !f.Postponed);

        if (fixture is null)
        {
            EngineStatus.Text = "No unplayed fixture is available.";
            return;
        }

        if (!int.TryParse(MatchMinutesEntry.Text, out var matchMinutes))
            matchMinutes = 90;
        if (!int.TryParse(DurationEntry.Text, out var durationSeconds))
            durationSeconds = 8;
        if (!int.TryParse(HighlightsEntry.Text, out var highlightCount))
            highlightCount = 10;

        matchMinutes = Math.Clamp(matchMinutes, 1, 120);
        durationSeconds = Math.Clamp(durationSeconds, 0, 300);
        highlightCount = Math.Clamp(highlightCount, 0, 50);

        var response = _api.Handle("POST", GameApi.SimulatePath,
            JsonSerializer.Serialize(new
            {
                fixtureId = fixture.Id,
                matchMinutes,
                durationSeconds,
                includeHighlights = IncludeHighlightsSwitch.IsToggled,
                highlightCount
            }, _game.JsonOptions));

        ApiOutput.Text = PrettyJson(response.Body);

        if (response.StatusCode == 200)
        {
            using var doc = JsonDocument.Parse(response.Body);
            var result = doc.RootElement.GetProperty("result");
            EngineStatus.Text =
                $"Result: {team.ShortName} match simulated — " +
                $"{result.GetProperty("homeGoals").GetInt32()}–{result.GetProperty("awayGoals").GetInt32()}. " +
                "Highlights and cup rules were processed by the engine.";
            SaveStatus.Text = "Match result and updated statistics saved to SQLite.";
            RefreshDashboard();
        }
        else
        {
            EngineStatus.Text = $"Simulation error: HTTP {response.StatusCode}.";
        }
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        _game.Save("default");
        SaveStatus.Text = $"Saved to SQLite: {_game.State.CurrentDateUtc:dd MMM yyyy HH:mm}.";
    }

    private void RefreshDashboard()
    {
        SeasonLabel.Text = _game.State.Season.ToString();
        FixtureCountLabel.Text = _game.State.Fixtures.Count.ToString();

        var gameResponse = _api.Handle("GET", GameApi.GamePath);
        ApiOutput.Text = PrettyJson(gameResponse.Body);

        var team = _game.GetPlayerTeam();
        if (team is null)
        {
            NextFixtureLabel.Text = "Select a club to see its next fixture.";
            return;
        }

        RefreshFormationPicker();

        var next = _game.Fixtures(teamId: team.Id)
            .FirstOrDefault(f => !f.IsPlayed && !f.Postponed);

        if (next is null)
        {
            NextFixtureLabel.Text = $"{team.Name}: no upcoming fixture.";
            return;
        }

        var home = _game.State.Teams[next.HomeTeamId];
        var away = _game.State.Teams[next.AwayTeamId];
        var competition = _game.State.Competitions.TryGetValue(next.CompetitionId, out var c) ? c.Name : next.CompetitionId;

        NextFixtureLabel.Text =
            $"{competition}: {next.DateUtc:ddd dd MMM} — {home.ShortName} vs {away.ShortName}";
    }

    private string PrettyJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private sealed record TeamChoice(string Id, string DisplayName);
    private sealed record FormationChoice(Formation Formation, string DisplayName);
    private sealed record TeamListResponse(List<TeamDto> Teams);
    private sealed record TeamDto(string Id, string Name, string ShortName, string LeagueId);
}
