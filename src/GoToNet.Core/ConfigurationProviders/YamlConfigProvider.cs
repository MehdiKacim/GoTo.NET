using GoToNet.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoToNet.Core.Models.Configuration;
using YamlDotNet.Serialization;

namespace GoToNet.Core.Services.ConfigurationProviders
{
    public class YamlConfigProvider : IAppNavigationCatalog, IDesignFlowRulesProvider
    {
        private readonly string _configFilePath;
        private GoToNetConfig? _loadedConfig;
        private IDeserializer _deserializer;

        private List<string> _allAvailablePagesCache = new List<string>();
        private List<string> _mainNavigationItemsCache = new List<string>();

        public YamlConfigProvider(string configFilePath)
        {
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            _deserializer = new DeserializerBuilder().Build();
        }

        public async Task LoadConfigAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                Console.WriteLine($"[YamlConfigProvider] Configuration file not found: {_configFilePath}");
                _loadedConfig = new GoToNetConfig();
                _allAvailablePagesCache = new List<string>();
                _mainNavigationItemsCache = new List<string>();
                return;
            }

            try
            {
                var yaml = await File.ReadAllTextAsync(_configFilePath);
                _loadedConfig = _deserializer.Deserialize<GoToNetConfig>(yaml);

                if (_loadedConfig == null)
                {
                    Console.WriteLine($"[YamlConfigProvider] Empty or invalid YAML file: {_configFilePath}");
                    _loadedConfig = new GoToNetConfig();
                }

                _allAvailablePagesCache = _loadedConfig.AppPages ?? new List<string>();
                _allAvailablePagesCache = _allAvailablePagesCache.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList();

                _mainNavigationItemsCache = _loadedConfig.MainNavigationItems ?? new List<string>();
                // Optional: ensure main items are actually in the global catalog
                _mainNavigationItemsCache = _mainNavigationItemsCache.Where(p => _allAvailablePagesCache.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();


                Console.WriteLine($"[YamlConfigProvider] Configuration loaded from: {_configFilePath}.");
                Console.WriteLine($"[YamlConfigProvider] App pages found: {_allAvailablePagesCache.Count}.");
                Console.WriteLine($"[YamlConfigProvider] Design flows found: {_loadedConfig.DesignFlows?.Count ?? 0}.");
                Console.WriteLine($"[YamlConfigProvider] Main navigation items found: {_mainNavigationItemsCache.Count}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YamlConfigProvider] Error loading YAML configuration: {ex.Message}");
                _loadedConfig = new GoToNetConfig();
                _allAvailablePagesCache = new List<string>();
                _mainNavigationItemsCache = new List<string>();
            }
        }

        public IEnumerable<string> GetAllAvailableNavigationItems()
        {
            return _allAvailablePagesCache;
        }

        public IEnumerable<string> GetMainNavigationItems()
        {
            return _mainNavigationItemsCache;
        }

        public Task<IReadOnlyDictionary<string, List<string>>> GetDesignFlowRulesAsync()
        {
            if (_loadedConfig?.DesignFlows == null)
            {
                Console.WriteLine("[YamlConfigProvider] Design flow rules not loaded or empty. Returning empty dictionary.");
                return Task.FromResult<IReadOnlyDictionary<string, List<string>>>(new Dictionary<string, List<string>>());
            }

            var rules = _loadedConfig.DesignFlows.ToDictionary(
                f => f.SourcePage,
                f => f.TargetPages,
                StringComparer.OrdinalIgnoreCase
            );
            return Task.FromResult<IReadOnlyDictionary<string, List<string>>>(rules);
        }
    }
}