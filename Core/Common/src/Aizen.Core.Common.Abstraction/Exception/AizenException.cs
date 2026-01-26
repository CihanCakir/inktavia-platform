using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Aizen.Core.Common.Abstraction.Exception
{
    [Serializable]
    public class AizenException : System.Exception
    {
        private static readonly object _sync = new();

        // Manuel atanmış değerler (ConfigureResource ile)
        private static Assembly? _resourceAssembly = null;
        private static string _resourceName = "Aizen.Modules.InktaviaStore.Resource.aizen_error_messages.json"; // varsayılan

        // Dinamik çözümleyici (ConfigureResourceResolver / ConfigureFromConfiguration ile set edilir)
        private static Func<(Assembly asm, string resourceName)>? _resourceResolver;

        // ---- PERF: O(1) erişim için sözlükler ----
        // errorCode -> ErrorViewModel
        private static Lazy<Dictionary<int, ErrorViewModel>> _errorMap = new(LoadErrorMap, isThreadSafe: true);

        // language -> (errorCode -> List<description>)
        private static Lazy<Dictionary<string, Dictionary<int, List<string>>>> _byLang =
            new(BuildLangIndex, isThreadSafe: true);

        public int ErrorCode { get; protected set; } = 9999; // default unknown
        public bool IsRollback { get; set; } = true;
        public ErrorViewModel? ErrorCustomMessage { get; protected set; }

        // ---------------------------
        // Kurulum yöntemleri
        // ---------------------------

        /// <summary>Gömülü kaynağı doğrudan assembly ve tam resource adıyla ayarla.</summary>
        public static void ConfigureResource(Assembly assembly, string embeddedResourceName)
        {
            lock (_sync)
            {
                _resourceAssembly = assembly;
                _resourceName = embeddedResourceName;
                _resourceResolver = null; // manuel ayar öncelikli
                ResetCaches();
            }
        }

        /// <summary>Kaynağı runtime’da dinamik belirlemek için resolver.</summary>
        public static void ConfigureResourceResolver(Func<(Assembly asm, string resourceName)> resolver)
        {
            lock (_sync)
            {
                _resourceResolver = resolver;
                ResetCaches();
            }
        }

        /// <summary>
        /// appsettings.json'daki ErrorLocalization bölümünden okur.
        /// Örn:
        /// "ErrorLocalization": {
        ///   "Assembly": "Aizen.Modules.InktaviaStore",
        ///   "ResourceName": "Aizen.Modules.InktaviaStore.Resource.aizen_error_messages.json",
        ///   "ResourceFolder": "Resource",
        ///   "ResourceFile": "aizen_error_messages.json"
        /// }
        /// </summary>
        public static void ConfigureFromConfiguration(
            IConfiguration cfg,
            Assembly? fallbackAssembly = null,
            string defaultFolder = "Resource",
            string defaultFile = "aizen_error_messages.json")
        {
            var section = cfg.GetSection("ErrorLocalization");
            var assemblyName = section["Assembly"];
            var resourceName = section["ResourceName"];
            var resourceFile = section["ResourceFile"] ?? defaultFile;
            var resourceFolder = section["ResourceFolder"] ?? defaultFolder;

            ConfigureResourceResolver(() =>
            {
                var asm = !string.IsNullOrWhiteSpace(assemblyName)
                    ? Assembly.Load(assemblyName!)
                    : (fallbackAssembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

                var names = asm.GetManifestResourceNames();

                // Tam ad verilmişse
                var res = !string.IsNullOrWhiteSpace(resourceName)
                    ? names.FirstOrDefault(n => n.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                    : null;

                // Değilse, "Namespace.Resource.aizen_error_messages.json" sonda olacak şekilde ara
                res ??= names.FirstOrDefault(n =>
                    n.EndsWith($".{resourceFolder}.{resourceFile}", StringComparison.OrdinalIgnoreCase));

                // Hâlâ yoksa ".Resource." içeren ve ".json" ile biten ilkini dene
                res ??= names.FirstOrDefault(n =>
                    n.Contains($".{resourceFolder}.", StringComparison.OrdinalIgnoreCase) &&
                    n.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

                // Son çare: varsayılan isim
                res ??= _resourceName;

                return (asm, res);
            });
        }

        private static void ResetCaches()
        {
            _errorMap = new Lazy<Dictionary<int, ErrorViewModel>>(LoadErrorMap, true);
            _byLang = new Lazy<Dictionary<string, Dictionary<int, List<string>>>>(BuildLangIndex, true);
        }

        // ---------------------------
        // Yükleyiciler
        // ---------------------------

        private static List<ErrorViewModel> LoadErrors()
        {
            Assembly asm;
            string name;

            if (_resourceResolver is not null)
            {
                (asm, name) = _resourceResolver();
            }
            else if (_resourceAssembly is not null)
            {
                asm = _resourceAssembly!;
                name = _resourceName;
            }
            else
            {
                // Entry veya Executing assembly üzerinde Resource/error.json’ı ara
                asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var names = asm.GetManifestResourceNames();
                name = names.FirstOrDefault(n =>
                           n.EndsWith(".Resource.aizen_error_messages.json", StringComparison.OrdinalIgnoreCase))
                       ?? _resourceName;
            }

            try
            {
                using var stream = asm.GetManifestResourceStream(name);
                if (stream is null) return new List<ErrorViewModel>();

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                return JsonSerializer.Deserialize<List<ErrorViewModel>>(json, opts) ?? new List<ErrorViewModel>();
            }
            catch
            {
                return new List<ErrorViewModel>();
            }
        }

        private static Dictionary<int, ErrorViewModel> LoadErrorMap()
        {
            var list = LoadErrors();
            return list
                .GroupBy(e => e.ErrorCode)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static Dictionary<string, Dictionary<int, List<string>>> BuildLangIndex()
        {
            var map = new Dictionary<string, Dictionary<int, List<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _errorMap.Value)
            {
                var code = kv.Key;
                var ev = kv.Value;
                if (ev?.Errors == null) continue;

                foreach (var d in ev.Errors)
                {
                    if (!map.TryGetValue(d.Language, out var perCode))
                        map[d.Language] = perCode = new Dictionary<int, List<string>>();

                    if (!perCode.TryGetValue(code, out var list))
                        perCode[code] = list = new List<string>();

                    list.Add(d.Description);
                }
            }
            return map;
        }

        // ---------------------------
        // API
        // ---------------------------

        public string GetErrorMessage(string language)
        {
            // Önce custom (validation aggregate, runtime-fallback vb.)
            if (ErrorCustomMessage?.Errors?.Count > 0)
            {
                var items = ErrorCustomMessage.Errors
                    .Where(x => language.Equals(x.Language, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Description)
                    .ToList();

                if (items.Count == 0)
                    return ErrorCustomMessage.Errors.First().Description;

                return items.Count == 1 ? items[0] : string.Join(Environment.NewLine, items);
            }

            // Sonra map'lerden O(1)
            if (_byLang.Value.TryGetValue(language, out var perCode) &&
                perCode.TryGetValue(ErrorCode, out var texts) &&
                texts.Count > 0)
            {
                return texts.Count == 1 ? texts[0] : string.Join(Environment.NewLine, texts);
            }

            // Dil yoksa default mesaja düş
            if (_errorMap.Value.TryGetValue(ErrorCode, out var ev2) && ev2.Errors?.Count > 0)
                return ev2.Errors.First().Description;

            return Message ?? $"ErrorCode: {ErrorCode}";
        }

        // ---------------------------
        // Ctor’lar
        // ---------------------------

        public AizenException(int errorCode)
            : base()
        {
            ErrorCode = errorCode;
            ErrorCustomMessage = _errorMap.Value.TryGetValue(errorCode, out var ev)
                ? ev
                : Fallback(errorCode, $"Error code: {errorCode}");
        }

        public AizenException(string message)
            : base(message)
        {
            if (TryParseSingle(message, out var code))
            {
                ErrorCode = code;
                ErrorCustomMessage = _errorMap.Value.TryGetValue(code, out var ev)
                    ? ev
                    : Fallback(code, message);
            }
            else if (TryParseMultiple(message, out var codes))
            {
                ErrorCode = 8888;
                ErrorCustomMessage = AggregateValidation(codes);
            }
            else
            {
                ErrorCode = 9999;
                ErrorCustomMessage = Fallback(9999, message);
            }
        }

        public AizenException(int errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorCustomMessage = _errorMap.Value.TryGetValue(errorCode, out var ev)
                ? ev
                : Fallback(errorCode, message);
        }

        public AizenException(string message, System.Exception inner)
            : base(message, inner)
        {
            if (TryParseSingle(message, out var code))
            {
                ErrorCode = code;
                ErrorCustomMessage = _errorMap.Value.TryGetValue(code, out var ev)
                    ? ev
                    : Fallback(code, message);
            }
            else if (TryParseMultiple(message, out var codes))
            {
                ErrorCode = 8888;
                ErrorCustomMessage = AggregateValidation(codes);
            }
            else
            {
                ErrorCode = 9999;
                ErrorCustomMessage = Fallback(9999, message);
            }
        }

        public AizenException(int errorCode, string message, System.Exception inner)
            : base(message, inner)
        {
            ErrorCode = errorCode;
            ErrorCustomMessage = _errorMap.Value.TryGetValue(errorCode, out var ev)
                ? ev
                : Fallback(errorCode, message);
        }

        protected AizenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetInt32(nameof(ErrorCode));
            IsRollback = info.GetBoolean(nameof(IsRollback));
            var json = info.GetString(nameof(ErrorCustomMessage));
            if (!string.IsNullOrWhiteSpace(json))
                ErrorCustomMessage = JsonSerializer.Deserialize<ErrorViewModel>(json);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(IsRollback), IsRollback);
            info.AddValue(nameof(ErrorCustomMessage), JsonSerializer.Serialize(ErrorCustomMessage));
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private static bool TryParseSingle(string message, out int code)
            => int.TryParse(message, out code);

        private static bool TryParseMultiple(string message, out IEnumerable<int> codes)
        {
            codes = Array.Empty<int>();
            if (string.IsNullOrWhiteSpace(message)) return false;

            var parts = message.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length <= 1) return false;

            var list = new List<int>();
            foreach (var p in parts)
            {
                if (!int.TryParse(p, out var c)) return false;
                list.Add(c);
            }
            codes = list;
            return true;
        }

        private static ErrorViewModel Fallback(int errorCode, string message) => new()
        {
            ErrorCode = errorCode,
            Errors = new List<ErrorDescriptionModel>
            {
                new() { Language = "TR", Description = message },
                new() { Language = "EN", Description = message }
            }
        };

        private static ErrorViewModel AggregateValidation(IEnumerable<int> codes)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in codes)
            {
                if (!_errorMap.Value.TryGetValue(code, out var ev) || ev.Errors == null) continue;

                foreach (var d in ev.Errors)
                {
                    if (!dict.TryGetValue(d.Language, out var list))
                        dict[d.Language] = list = new List<string>();
                    list.Add(d.Description);
                }
            }

            return new ErrorViewModel
            {
                ErrorCode = 8888,
                Errors = dict.SelectMany(kv => kv.Value.Select(v => new ErrorDescriptionModel
                {
                    Language = kv.Key,
                    Description = v
                })).ToList()
            };
        }
    }

    // ---------------------------
    // Basit modeller (projende yoksa ekle)
    // ---------------------------
    public class ErrorViewModel
    {
        public int ErrorCode { get; set; }
        public List<ErrorDescriptionModel> Errors { get; set; } = new();
    }

    public class ErrorDescriptionModel
    {
        public string Language { get; set; } = "TR";
        public string Description { get; set; } = "";
    }
}
