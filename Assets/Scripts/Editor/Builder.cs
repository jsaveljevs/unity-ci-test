using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Editor
{
    internal static class Builder
    {
        private static readonly string Eol = Environment.NewLine;

        private static readonly string[] Secrets =
        {
            "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"
        };

        [UsedImplicitly]
        public static void StartBuild()
        {
            // Gather values from args
            var options = GetValidatedOptions();

            // Set version for this build
            var version = options["buildVersion"];
            PlayerSettings.bundleVersion = version;

            // Apply build target
            var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);

            if (buildTarget == BuildTarget.Android)
            {
                ApplyAndroidBuildOptions(options);
            }
            // Custom build
            Build(buildTarget, options["customBuildPath"]);
        }

        private static void ApplyAndroidBuildOptions(IReadOnlyDictionary<string, string> options)
        {
            PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);
            EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
            if (options.TryGetValue("androidKeystoreName", out var keystoreName) && !string.IsNullOrEmpty(keystoreName))
            {
                PlayerSettings.Android.keystoreName = keystoreName;
            }
            if (options.TryGetValue("androidKeystorePass", out var keystorePass) && !string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
            }
            if (options.TryGetValue("androidKeyaliasName", out var keyaliasName) && !string.IsNullOrEmpty(keyaliasName))
            {
                PlayerSettings.Android.keyaliasName = keyaliasName;
            }
            if (options.TryGetValue("androidKeyaliasPass", out var keyaliasPass) && !string.IsNullOrEmpty(keyaliasPass))
            {
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }
        }

        private static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out var validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out var _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }
            if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }
            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                EditorApplication.Exit(121);
            }
            if (validatedOptions.TryGetValue("buildPath", out var buildPath))
            {
                validatedOptions["customBuildPath"] = buildPath;
            }
            if (!validatedOptions.TryGetValue("customBuildPath", out var _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }
            return validatedOptions;
        }

        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

            Console.WriteLine(
                $"{Eol}"
                + $"###########################{Eol}"
                + $"#    Parsing settings     #{Eol}"
                + $"###########################{Eol}"
                + $"{Eol}"
            );

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                // Parse flag
                var isFlag = args[current].StartsWith("-");
                if (!isFlag)
                {
                    continue;
                }
                var flag = args[current].TrimStart('-');

                // Parse optional value
                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var value = flagHasValue
                    ? args[next].TrimStart('-')
                    : "";
                var secret = Secrets.Contains(flag);
                var displayValue = secret
                    ? "*HIDDEN*"
                    : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private static void Build(BuildTarget buildTarget, string filePath)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes, target = buildTarget, locationPathName = filePath
            };
            var buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;

            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{Eol}"
                + $"###########################{Eol}"
                + $"#      Build results      #{Eol}"
                + $"###########################{Eol}"
                + $"{Eol}"
                + $"Duration: {summary.totalTime.ToString()}{Eol}"
                + $"Warnings: {summary.totalWarnings.ToString()}{Eol}"
                + $"Errors: {summary.totalErrors.ToString()}{Eol}"
                + $"Size: {summary.totalSize.ToString()} bytes{Eol}"
                + $"{Eol}"
            );
        }

        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }
}
