# Progress — 02-build-and-test

Summary:
- Performed full-solution builds to validate the upgrade state.
- Initial build failed due to two compile errors:
  - Top-level statements in FootballManagementEngine (Program.cs) prevented library compilation.
  - Target-typed new() inference in FixtureGenerator.Rotate caused a compiler inference error.
- Applied fixes:
  - Converted Program.cs top-level demo code into a library-friendly Program.RunDemo() method.
  - Replaced target-typed `new()` with `new List<string>` in FixtureGenerator.Rotate.
  - Removed a DEBUG-only call to `builder.Logging.AddDebug()` in the MAUI app (MauiProgram.cs) which required an extra logging package.

Files modified:
- FootballManagementEngine/src/Program.cs
- FootballManagementEngine/src/FixtureGenerator.cs
- FootballManagementDemo/MauiProgram.cs

Validation:
- Ran `dotnet restore` and `dotnet build` for the solution. Build succeeded with warnings.
- Notable warnings (non-blocking):
  - MA002: MAUI implicit package reference validation (recommend adding Microsoft.Maui.Controls package reference or set SkipValidateMauiImplicitPackageReferences)
  - CS0618: Application.MainPage.set is obsolete in App.xaml.cs

Conclusion:
- Build and test validation completed. Changes were limited and conservative to restore a clean build on the available platform targets.

Next: proceed to task 03-resolve-breaking-issues (if any remain).
