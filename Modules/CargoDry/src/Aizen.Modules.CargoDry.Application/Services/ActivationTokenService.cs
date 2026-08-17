using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Aizen.Modules.CargoDry.Application.Services;

public sealed class ActivationTokenService : IActivationTokenService
{
    private const int TtlMinutes = 5;
    private readonly string _secret;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ActivationTokenService> _logger;

    public ActivationTokenService(
        IConfiguration cfg,
        IConnectionMultiplexer redis,
        ILogger<ActivationTokenService> logger)
    {
        _secret = cfg["CargoDry:ActivationTokenSecret"]
            ?? throw new InvalidOperationException("CargoDry:ActivationTokenSecret not configured");
        _redis  = redis;
        _logger = logger;
    }

    public string Generate(string serialNumber)
    {
        var jti     = Guid.NewGuid().ToString("N");
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var handler = new JwtSecurityTokenHandler();
        var token   = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("serial", serialNumber),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            ]),
            Expires            = DateTime.UtcNow.AddMinutes(TtlMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
        var tokenStr = handler.WriteToken(token);

        var db = _redis.GetDatabase();
        db.StringSet($"cargodry:jti:{jti}", "1", TimeSpan.FromMinutes(TtlMinutes + 1));

        return tokenStr;
    }

    public ActivationTokenClaims? Verify(string token)
    {
        try
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer   = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew        = TimeSpan.Zero,
            }, out var validated);

            var jwt    = (JwtSecurityToken)validated;
            var serial = jwt.Claims.First(c => c.Type == "serial").Value;
            var jti    = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            var db     = _redis.GetDatabase();
            var exists = (bool)db.ScriptEvaluate(
                "local v = redis.call('GET', KEYS[1]) if v then redis.call('DEL', KEYS[1]) return 1 else return 0 end",
                [$"cargodry:jti:{jti}"]);

            if (!exists)
            {
                _logger.LogWarning("CargoDry activation token JTI already consumed: {Jti}", jti);
                return null;
            }

            return new ActivationTokenClaims
            {
                SerialNumber = serial,
                Jti          = jti,
                ExpiresAt    = DateTimeOffset.FromUnixTimeSeconds(jwt.Payload.Exp ?? 0),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CargoDry activation token verification failed");
            return null;
        }
    }
}
