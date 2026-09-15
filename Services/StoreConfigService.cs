using System.Text.Json;

namespace EcommerceApp.Services
{
    public class StoreSettingsModel
    {
        public string BrandName { get; set; } = "NOVA";
        public string LogoInitial { get; set; } = "N";
        public string Tagline { get; set; } = "Celulares · Audio High-End";
        public string Announcement { get; set; } = "🔥 Envíos gratis a todo el país en compras superiores a $199.000 · 12 cuotas sin interés";
        public string SupportPhone { get; set; } = "+54 11 4500-8000";
        public string SupportEmail { get; set; } = "contacto@novastore.com";
        public decimal FreeShippingThreshold { get; set; } = 199000;
        public string Instagram { get; set; } = "@nova.tech";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class StoreConfigService
    {
        private readonly string _filePath;
        private StoreSettingsModel _settings;
        private readonly object _lock = new();

        public StoreConfigService(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "storesettings.json");
            _settings = LoadSettings();
        }

        public StoreSettingsModel GetSettings()
        {
            lock (_lock)
            {
                return _settings;
            }
        }

        public void UpdateSettings(StoreSettingsModel newSettings)
        {
            lock (_lock)
            {
                newSettings.LastUpdated = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(newSettings.LogoInitial) && !string.IsNullOrWhiteSpace(newSettings.BrandName))
                {
                    newSettings.LogoInitial = newSettings.BrandName.Trim()[..1].ToUpper();
                }
                _settings = newSettings;
                try
                {
                    var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, json);
                }
                catch
                {
                    // Fallback safely in case of disk permission limits
                }
            }
        }

        private StoreSettingsModel LoadSettings()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var parsed = JsonSerializer.Deserialize<StoreSettingsModel>(json);
                    if (parsed != null) return parsed;
                }
            }
            catch
            {
            }
            return new StoreSettingsModel();
        }
    }
}
