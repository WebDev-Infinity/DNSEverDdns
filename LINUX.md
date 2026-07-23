# DNSEver DDNS Linux Client

DNSEver DDNS Linux Client는 기존 `DNSEverDdns.Core`를 사용하는 명령줄 기반 클라이언트입니다. GUI 없이 DDNS를 즉시 업데이트하거나 systemd 사용자 서비스로 주기적으로 실행할 수 있습니다.

## 지원 환경

- x64 Linux: `linux-x64`
- ARM64 Linux: `linux-arm64`
- 런타임: .NET 10 Runtime
- 서비스 관리자: systemd user service

현재 게시 파일은 framework-dependent 단일 실행 파일이므로 대상 장치에 아키텍처와 일치하는 .NET 10 Runtime이 필요합니다.

## 설치

사용 중인 아키텍처에 맞는 실행 파일을 다운로드한 뒤 영구적으로 사용할 폴더에 저장합니다.

```bash
mkdir -p ~/.local/bin
mv DNSEverDdns-1.0.2026.0724-linux-x64 ~/.local/bin/
chmod +x ~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64
```

ARM64 장치에서는 파일명의 `linux-x64`를 `linux-arm64`로 바꿉니다.

systemd 서비스에는 현재 실행 파일의 전체 경로가 등록됩니다. 서비스를 등록한 후 실행 파일을 이동하면 서비스가 시작되지 않으므로 먼저 최종 경로로 이동해야 합니다.

## 초기 설정

다음 명령을 실행합니다.

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 configure
```

프로그램에서 다음 정보를 순서대로 입력합니다.

1. DNSEver 사용자 아이디
2. 다이나믹 DNS 인증 코드
3. 업데이트할 호스트 번호
4. 지정 IP 또는 자동 감지
5. 업데이트 주기

인증에 성공하면 DNSEver에 등록된 호스트 목록이 표시됩니다.

설정은 다음 위치에 저장됩니다.

```text
${XDG_CONFIG_HOME:-$HOME/.config}/DNSEverDdns/settings.json
```

Linux에서 운영체제 공통 암호화 저장소를 강제하지 않기 때문에 인증 코드는 설정 파일에 저장됩니다. 프로그램은 설정 디렉터리를 `0700`, 설정 파일을 `0600` 권한으로 제한합니다. 해당 사용자 계정과 시스템 관리자만 설정을 읽을 수 있습니다.

## 즉시 업데이트

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 update
```

DNSEver 응답에서 `720 Update Success`와 `721 Already Updated`는 모두 정상 상태로 처리됩니다.

## 현재 설정 확인

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 status
```

인증 코드는 출력하지 않습니다.

## 터미널에서 계속 실행

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 run
```

종료하려면 `Ctrl+C`를 누릅니다.

## systemd 사용자 서비스

초기 설정을 완료한 후 다음 명령으로 현재 사용자 서비스에 등록하고 즉시 시작합니다.

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 install-service
```

관리자 권한은 필요하지 않습니다. 서비스 상태는 다음 명령으로 확인합니다.

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 service-status
```

로그는 journalctl로 확인할 수 있습니다.

```bash
journalctl --user -u dnsever-ddns.service -f
```

서비스를 중지하고 등록을 해제하려면 다음 명령을 실행합니다.

```bash
~/.local/bin/DNSEverDdns-1.0.2026.0724-linux-x64 uninstall-service
```

systemd 사용자 서비스는 기본적으로 사용자가 로그인한 동안 실행됩니다. 로그인하지 않은 상태에서도 실행하려면 시스템 관리자가 해당 사용자에 대해 linger 정책을 별도로 구성해야 합니다.

## Linux 게시

```powershell
dotnet publish .\DNSEverDdns.Linux\DNSEverDdns.Linux.csproj -c Release -r linux-x64 --self-contained false -o .\publish-artifacts\linux-x64
dotnet publish .\DNSEverDdns.Linux\DNSEverDdns.Linux.csproj -c Release -r linux-arm64 --self-contained false -o .\publish-artifacts\linux-arm64
```

게시 결과는 다음 이름으로 생성됩니다.

```text
DNSEverDdns-1.0.2026.0724-linux-x64
DNSEverDdns-1.0.2026.0724-linux-arm64
```
