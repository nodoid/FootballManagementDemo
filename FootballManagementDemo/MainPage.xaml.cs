using System.Text.Json;
using FootballManagementEngine;

namespace FootballManagementDemo;

public partial class MainPage : ContentPage
{
    private readonly FootballGameEngine _game;
    private readonly GameApi _api;
    private readonly SeasonEngine _season;
    private readonly MatchSimulator _simulator;

    private readonly List<TeamChoice> _teams = new();

    public MainPage()
    {
        InitializeComponent();

        _game = UkDatabase.Create();
        _season = new SeasonEngine(_game);
        _simulator = new MatchSimulator();

        // Seed a representative season so the UI can demonstrate the engine.
        _season.GenerateDomesticSeason();
        _season.GenerateFaCup();

        _game.State.Competitions["UCL"].TeamIds.AddRange(
            new[] { "ARS", "LIV", "MCI", "MUN", "CHE", "NEW" });
        _game.State.Competitions["UEL"].TeamIds.AddRange(
            new[] { "AVL", "TOT", "BHA", "WHU" });
        _game.State.Competitions["UECL"].TeamIds.AddRange(
            new[] { "CRY", "FUL" });
        _season.GenerateEuropeanFixtures();

        _api = new GameApi(_game);

        LoadTeams();
        RefreshDashboard();
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
    }

    private void OnSelectTeamClicked(object sender, EventArgs e)
    {
        if (TeamPicker.SelectedItem is not TeamChoice choice)
        {
            SelectionStatus.Text = "Choose a club first.";
            return;
        }

        var request = JsonSerializer.Serialize(
            new { teamId = choice.Id },
            _game.JsonOptions);

        var response = _api.Handle("POST", GameApi.SelectTeamPath, request);

        ApiOutput.Text = PrettyJson(response.Body);

        if (response.StatusCode == 200)
        {
            var team = _game.GetPlayerTeam();
            SelectionStatus.Text = $"Now managing {team?.Name} ({team?.ShortName}).";
            EngineStatus.Text = $"API returned HTTP {response.StatusCode}. Selection persisted in GameState.";
            RefreshDashboard();
        }
        else
        {
            SelectionStatus.Text = $"API error: HTTP {response.StatusCode}.";
        }
    }

    private void OnAdvanceWeekClicked(object sender, EventArgs e)
    {
        _season.ProcessWeek();
        EngineStatus.Text = $"Advanced to {_game.State.CurrentDateUtc:dd MMM yyyy}. Weekly finance/injury processing completed.";
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

        var home = _game.State.Teams[fixture.HomeTeamId];
        var away = _game.State.Teams[fixture.AwayTeamId];
        var result = _simulator.Simulate(fixture, home, away);
        _game.ApplyResult(result);

        EngineStatus.Text =
            $"Result: {home.ShortName} {result.HomeGoals}–{result.AwayGoals} {away.ShortName}. " +
            "The result was applied through FootballGameEngine.";
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        SeasonLabel.Text = _game.State.Season.ToString();
        FixtureCountLabel.Text = _game.State.Fixtures.Count.ToString();

        var team = _game.GetPlayerTeam();
        var gameResponse = _api.Handle("GET", GameApi.GamePath);
        ApiOutput.Text = PrettyJson(gameResponse.Body);

        if (team is null)
        {
            NextFixtureLabel.Text = "Select a club to see its next fixture.";
            return;
        }

        var next = _game.Fixtures(teamId: team.Id)
            .FirstOrDefault(f => !f.IsPlayed && !f.Postponed);

        if (next is null)
        {
            NextFixtureLabel.Text = $"{team.Name}: no upcoming fixture.";
            return;
        }

        var home = _game.State.Teams[next.HomeTeamId];
        var away = _game.State.Teams[next.AwayTeamId];
        NextFixtureLabel.Text =
            $"{team.ShortName} next: {next.DateUtc:ddd dd MMM} — {home.ShortName} vs {away.ShortName}";
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

    private sealed record TeamListResponse(List<TeamDto> Teams);

    private sealed record TeamDto(string Id, string Name, string ShortName, string LeagueId);
}
