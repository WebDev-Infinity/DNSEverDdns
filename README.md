# DNSEver DDNS Windows Client

DNSEver DDNS Windows Client는 DNSEver의 다이나믹 DNS를 Windows 환경에서 간단히 관리하기 위한 C# 기반 데스크톱 클라이언트입니다. DNSEver에서 제공하던 Windows용 다이나믹 DNS 클라이언트 지원이 중단된 상황을 보완하기 위해 제작되었습니다.

이 프로젝트는 **DNSEver에 사전 허락을 받고 공개 저장소에 게시한 프로젝트**입니다. 다만 DNSEver의 공식 제품이나 공식 지원 도구는 아니며, DNSEver 서비스와 상표, API에 대한 권리는 DNSEver에 있습니다.

현재 배포 버전은 `1.0.2026.0724`입니다.

## 프로젝트 구성

- `DNSEverDdns.Core`: DNSEver DDNS API 호출, Basic 인증 헤더 생성, XML 응답 파싱, 결과 모델을 담당하는 핵심 라이브러리입니다.
- `DNSEverDdns.Win`: Windows Forms 기반 트레이 애플리케이션입니다. 사용자 설정, 호스트 조회, 주기적 DDNS 업데이트, 로그 기록을 처리합니다.

## 주요 기능

- DNSEver DDNS 인증 코드 기반 Basic 인증
- DNSEver에 등록된 호스트 목록 조회
- 업데이트 대상 호스트 선택
- 선택한 호스트의 DDNS 수동 및 주기적 업데이트
- DNSEver 서버가 감지한 현재 IP 사용
- 사용자가 지정한 IP로 DDNS 업데이트
- 현재 공인 IP 조회 및 지정 IP 자동 적용
- Windows 트레이 아이콘 상주 실행
- 시작 시 설정이 완료되어 있으면 트레이로 최소화 실행
- 사용자가 선택할 수 있는 Windows 시작프로그램 등록 및 해제
- Windows 자동 시작 시 설정 창을 표시하지 않고 트레이에서 실행
- 업데이트 주기 설정(기본 60분)
- 어두운 테마와 밝은 테마 선택
- 이미 동일한 IP가 반영된 `721 Already Updated` 응답을 정상 상태로 처리
- Windows DPAPI `CurrentUser` 범위로 인증 코드 암호화 저장
- `%LOCALAPPDATA%\DNSEverDdns\settings.json` 설정 저장
- `%LOCALAPPDATA%\DNSEverDdns\logs` 로그 기록
- 단일 파일 게시 설정 지원

## 설정 저장 방식

앱 설정은 현재 Windows 사용자 로컬 앱 데이터 폴더에 저장됩니다.

```text
%LOCALAPPDATA%\DNSEverDdns\settings.json
```

아이디, 호스트 목록, 지정 IP, 업데이트 주기, 테마 및 시작프로그램 선택 상태는 설정 파일에 저장됩니다. DDNS 인증 코드는 Windows DPAPI를 사용해 현재 Windows 사용자 계정 범위로 암호화한 뒤 `ProtectedAuthCode` 값으로 저장됩니다.

다른 PC 또는 다른 Windows 사용자 계정으로 설정 파일만 복사하면 인증 코드는 일반적으로 복호화되지 않습니다.

시작프로그램은 현재 사용자 계정의 다음 레지스트리 경로에 등록되므로 관리자 권한이 필요하지 않습니다.

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

등록된 명령은 현재 실행 파일의 경로와 `--tray` 옵션을 사용합니다. 시작프로그램 등록 후 실행 파일을 다른 폴더로 이동했다면 새 위치에서 프로그램을 실행하고 설정을 다시 저장해야 합니다.

## 빌드

```powershell
dotnet build .\DNSEverDdns.slnx
```

## 실행

```powershell
dotnet run --project .\DNSEverDdns.Win\DNSEverDdns.Win.csproj
```

자세한 초기 설정과 사용 방법은 [USAGE.md](USAGE.md)를 참고하세요.

처음 실행하면 설정 화면이 표시됩니다.

1. DNSEver 사용자 아이디를 입력합니다.
2. DNSEver DDNS 인증 코드를 입력합니다.
3. `호스트 조회` 버튼으로 DNSEver에 등록된 호스트 목록을 가져옵니다.
4. DDNS 업데이트 대상 호스트를 선택합니다.
5. 필요한 경우 지정 IP와 업데이트 주기를 설정합니다. 기본 업데이트 주기는 60분입니다.
6. Windows 로그인 시 자동으로 실행하려면 `시작프로그램 등록`을 선택합니다.
7. 저장하면 DDNS가 즉시 반영되고 트레이에서 주기적으로 업데이트가 수행됩니다.

## Windows 배포

```powershell
dotnet publish .\DNSEverDdns.Win\DNSEverDdns.Win.csproj -c Release -r win-x64 --self-contained false -o .\publish-artifacts\win-x64
dotnet publish .\DNSEverDdns.Win\DNSEverDdns.Win.csproj -c Release -r win-x86 --self-contained false -o .\publish-artifacts\win-x86
dotnet publish .\DNSEverDdns.Win\DNSEverDdns.Win.csproj -c Release -r win-arm64 --self-contained false -o .\publish-artifacts\win-arm64
```

현재 배포 파일은 다음과 같습니다.

- `DNSEverDdns-1.0.2026.0724-win-x64.exe`: 일반적인 64비트 Windows PC
- `DNSEverDdns-1.0.2026.0724-win-x86.exe`: 32비트 Windows PC
- `DNSEverDdns-1.0.2026.0724-win-arm64.exe`: Windows on ARM 장치

Windows Forms 프로젝트의 `PublishSingleFile` 옵션을 사용하므로 아키텍처별로 하나의 실행 파일이 생성됩니다. 다만 framework-dependent 방식으로 게시되므로 실행 PC에 사용 중인 아키텍처와 일치하는 **.NET 10 Desktop Runtime**이 필요합니다.

## 1.0.2026.0724 변경 사항

- 설정 화면에 `시작프로그램 등록` 체크박스를 추가했습니다.
- Windows 로그인 시 `--tray` 옵션으로 자동 실행할 수 있습니다.
- DNSEver의 `721 Already Updated` 호스트 응답을 실패가 아닌 정상 반영 상태로 처리합니다.
- 긴 상태 메시지 때문에 입력 컨트롤과 버튼이 오른쪽으로 밀리던 UI 문제를 수정했습니다.
- 제품 및 파일 버전을 `1.0.2026.724`, 게시 파일명 버전을 `1.0.2026.0724`로 변경했습니다.
- 버전 설정을 [Directory.Build.props](Directory.Build.props)에서 통합 관리합니다.

자세한 변경 이력은 [CHANGELOG.md](CHANGELOG.md)를 참고하세요.

## 참고

DNSEver 문서 기준으로 너무 잦은 업데이트는 `320: Too Many updates sent` 응답을 받을 수 있습니다. 이 프로그램은 업데이트 주기를 최소 1분 이상으로 제한하지만, 실제 운영 환경에서는 DNS 변경이 필요한 범위 안에서 적절한 주기를 선택하는 것을 권장합니다.

DNSEver가 `721: Already Updated`를 반환하면 요청한 IP가 이미 해당 호스트에 적용된 상태이므로 정상적으로 처리됩니다.

## 라이선스

이 프로젝트의 소스 코드는 [MIT License](LICENSE)에 따라 공개됩니다.

DNSEver의 서비스, API, 상표, 문서, 기타 DNSEver가 보유한 권리는 이 저장소의 라이선스 적용 대상이 아닙니다.
