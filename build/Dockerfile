# syntax=docker/dockerfile:1
#
# Inktavia — TÜM .NET bileşenleri için tek Dockerfile (11 modül + 4 BFF).
#
# Build context: REPO KÖKÜ. Pazarlık konusu değil — modüller hem Core/ hem birbirlerinin
# Abstraction projelerine referans veriyor (ör. Profile -> Payment.Abstraction). Bileşenin
# kendi dizinini context almak derlemeyi kırar; Modules/Profile/build/Dockerfile tam olarak
# bu yüzden hiç çalışmıyordu.
#
# Neden 15 ayrı dosya değil de tek dosya: 15 kopyayı senkronda tutmak imkânsız. Bileşene özel
# ne varsa build-arg olarak geliyor, kaynağı build/components.json.
#
# Kullanım:
#   docker build -f build/Dockerfile \
#     --build-arg PROJECT=Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj \
#     --build-arg DLL=Aizen.Modules.Identity.dll .
#
# ⚠️ Mac (Apple Silicon) üzerinde elle build ederken `--platform linux/amd64` ŞART.
#    Aksi hâlde arm64 imaj üretirsin ve sunucuda "exec format error" alırsın.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT
WORKDIR /src

COPY Core ./Core
COPY Modules ./Modules
COPY Bff ./Bff

# Tek adım: `dotnet publish` zaten restore + build yapar.
# Eski Dockerfile'lar önce `dotnet build` sonra `dotnet publish` çalıştırıyordu; ikincisi
# birincinin işini baştan yapıyordu, yani derleme süresinin yarısı boşa gidiyordu.
# sharing=locked ZORUNLU: compose 15 servisi paralel derliyor ve hepsi AYNI cache
# mount'ini kullaniyor; varsayilan (shared) modda es-zamanli NuGet restore'lar paketleri
# yarim yaziyor ("Could not find file .../<rastgele>.mhn" — 2026-08-25'te yasandi).
# locked, mount'a ayni anda tek build'in yazmasini garanti eder; ilk soguk derleme
# serilesir ama cache dolunca fark onemsizlesir.
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "${PROJECT}" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
ARG DLL
ENV APP_DLL=${DLL}
ENV ASPNETCORE_HTTP_PORTS=8080

# .NET bellek ayarı — roadmap §1.6. Server GC çekirdek başına ayrı heap ayırıyor;
# 6 çekirdekli tek makinede 15 servis için bu tek başına ~1,5 GB fark ediyor.
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=5

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# ENTRYPOINT'in exec biçimi değişken genişletmez; `exec` ile dotnet'i PID 1 yapıyoruz ki
# SIGTERM doğrudan uygulamaya ulaşsın (aksi hâlde k8s pod'u 30 sn bekleyip SIGKILL atar).
ENTRYPOINT ["sh", "-c", "exec dotnet /app/${APP_DLL}"]
