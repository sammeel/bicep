// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core;
using Bicep.Core.Configuration;
using Bicep.LanguageServer.Configuration;
using Bicep.LanguageServer.Telemetry;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Bicep.LanguageServer.Handlers
{
    /// <summary>
    /// Handles the internal command for code actions to edit a particular linter rule in the bicepconfig.json file
    /// </summary>
    public class BicepEditLinterRuleCommandHandler : ExecuteTypedCommandHandlerBase<DocumentUri, string, string>
    {
        private readonly string DefaultBicepConfig;
        private readonly ILanguageServerFacade server;
        private readonly ITelemetryProvider telemetryProvider;

        public BicepEditLinterRuleCommandHandler(ISerializer serializer, ILanguageServerFacade server, ITelemetryProvider telemetryProvider)
            : base(LanguageConstants.EditLinterRuleCommandName, serializer)
        {
            DefaultBicepConfig = DefaultBicepConfigHelper.GetDefaultBicepConfig();
            this.server = server;
            this.telemetryProvider = telemetryProvider;
        }

        public override async Task<Unit> Handle(DocumentUri documentUri, string ruleCode, string bicepConfigFilePath, CancellationToken cancellationToken)
        {
            string? error = "unknown";
            bool newConfigFile = false;
            try
            {
                // bicepConfigFilePath will be empty string if no current configuration file was found
                if (string.IsNullOrEmpty(bicepConfigFilePath))
                {
                    // There is no configuration file currently - create one in the default location
                    var targetFolder = await BicepGetRecommendedConfigLocationHandler.GetRecommendedConfigFileLocation(this.server, documentUri.GetFileSystemPath());
                    bicepConfigFilePath = Path.Combine(targetFolder, LanguageConstants.BicepConfigurationFileName);
                }

                try
                {
                    if (!File.Exists(bicepConfigFilePath))
                    {
                        newConfigFile = true;
                        File.WriteAllText(bicepConfigFilePath, DefaultBicepConfig);
                    }
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name;
                    server.Window.ShowError($"Unable to create configuration file \"{bicepConfigFilePath}\": {ex.Message}");
                    return Unit.Value;
                }


                await AddAndSelectRuleLevel(bicepConfigFilePath, ruleCode);

                error = null;
                return Unit.Value;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name;
                server.Window.ShowError(ex.Message);
                return Unit.Value;
            }
            finally
            {
                telemetryProvider.PostEvent(BicepTelemetryEvent.EditLinterRule(ruleCode, newConfigFile, error));
            }
        }

        public async Task AddAndSelectRuleLevel(string bicepConfigFilePath, string ruleCode)
        {
            if (await SelectRuleLevelIfExists(ruleCode, bicepConfigFilePath))
            {
                // The rule already exists and has been shown/selected
                return;
            }

            string json = File.ReadAllText(bicepConfigFilePath);
            (int line, int column, string text)? insertion = new JsonEditor(json).InsertIfNotExist(
                new string[] { "analyzers", "core", "rules", ruleCode },
                new { level = "warning" });

            if (insertion.HasValue)
            {
                var (line, column, insertText) = insertion.Value;
                try
                {
                    File.WriteAllText(bicepConfigFilePath, JsonEditor.ApplyInsertion(json, (line, column, insertText)));
                }
                catch (Exception ex)
                {
                    server.Window.ShowError($"Unable to write to configuration file \"{bicepConfigFilePath}\": {ex.Message}");
                }

                await SelectRuleLevelIfExists(ruleCode, bicepConfigFilePath);
            }
        }

        /// <summary>
        /// If the given rule has an entry for its error level in the configuration file, show that file and select the current
        /// level value (so that the end user can immediatey edit it).
        /// </summary>
        /// <param name="ruleCode"></param>
        /// <param name="configFilePath"></param>
        /// <returns>True if the rule exists and displaying/highlighting succeeds, otherwise false.</returns>
        private async Task<bool> SelectRuleLevelIfExists(string ruleCode, string configFilePath)
        {
            // Inspect the JSON to figure out the location of the rule's level value
            Range? rangeOfRuleLevelValue = FindRangeOfPropertyValueInJson($"analyzers.core.rules.{ruleCode}.level", configFilePath);
            if (rangeOfRuleLevelValue is not null)
            {
                // Show the document first and allow the dust to settle
                await server.Window.ShowDocument(new()
                {
                    Uri = DocumentUri.File(configFilePath),
                });

                // Now show the document with our desired selection
                await server.Window.ShowDocument(new()
                {
                    Uri = DocumentUri.File(configFilePath),
                    Selection = rangeOfRuleLevelValue,
                    TakeFocus = true
                });

                return true;
            }

            return false;
        }

        private static Range? FindRangeOfPropertyValueInJson(string propertyPath, string jsonPath)
        {
            using TextReader textReader = File.OpenText(jsonPath);
            using JsonReader jsonReader = new JsonTextReader(textReader);

            var jObject = JObject.Load(jsonReader, new JsonLoadSettings
            {
                LineInfoHandling = LineInfoHandling.Load,
                CommentHandling = CommentHandling.Load
            });

            // Search for the given property path
            JToken? jToken = jObject?.SelectToken(propertyPath);
            if (jObject is not null && jToken is not null)
            {
                var lineInfo = jToken as IJsonLineInfo; // 1-indexed

                int line = lineInfo.LineNumber - 1;
                int column = lineInfo.LinePosition - 1 - jToken.ToString().Length;
                int length = jToken.ToString().Length;
                return new Range(line, column, line, column + length);
            }
            else
            {
                return null;
            }
        }
    }
}