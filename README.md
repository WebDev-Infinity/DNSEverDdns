# DNSEver DDNS Windows Client

DNSEver DDNS Windows Client는 DNSEver의 다이나믹 DNS를 Windows 환경에서 간단히 관리하기 위한 C# 기반 데스크톱 클라이언트입니다. DNSEver에서 제공하던 Windows용 다이나믹 DNS 클라이언트 지원이 중단된 상황을 보완하기 위해 제작되었습니다.

이 프로젝트는 **DNSEver에 사전 허락을 받고 공개 저장소에 게시한 프로젝트**입니다. 다만 DNSEver의 공식 제품이나 공식 지원 도구는 아니며, DNSEver 서비스와 상표, API에 대한 권리는 DNSEver에 있습니다.

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
- 업데이트 주기 설정(기본 60분)
- 어두운 테마와 밝은 테마 선택
- Windows DPAPI `CurrentUser` 범위로 인증 코드 암호화 저장
- `%LOCALAPPDATA%\DNSEverDdns\settings.json` 설정 저장
- `%LOCALAPPDATA%\DNSEverDdns\logs` 로그 기록
- 단일 파일 게시 설정 지원

## 설정 저장 방식

앱 설정은 현재 Windows 사용자 로컬 앱 데이터 폴더에 저장됩니다.

```text
%LOCALAPPDATA%\DNSEverDdns\settings.json
```

아이디, 호스트 목록, 지정 IP, 업데이트 주기, 테마 설정은 설정 파일에 저장됩니다. DDNS 인증 코드는 Windows DPAPI를 사용해 현재 Windows 사용자 계정 범위로 암호화한 뒤 `ProtectedAuthCode` 값으로 저장됩니다.

다른 PC 또는 다른 Windows 사용자 계정으로 설정 파일만 복사하면 인증 코드는 일반적으로 복호화되지 않습니다.

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
6. 저장하면 트레이에서 주기적으로 업데이트가 수행됩니다.

## 배포 예시

```powershell
dotnet publish .\DNSEverDdns.Win\DNSEverDdns.Win.csproj -c Release -r win-x64 --self-contained false
```

게시 설정은 Windows Forms 프로젝트의 `PublishSingleFile` 옵션을 사용합니다.
별도 설치가 필요 없는 단독 실행 파일입니다.  

## 참고

DNSEver 문서 기준으로 너무 잦은 업데이트는 `320: Too Many updates sent` 응답을 받을 수 있습니다. 이 프로그램은 업데이트 주기를 최소 1분 이상으로 제한하지만, 실제 운영 환경에서는 DNS 변경이 필요한 범위 안에서 적절한 주기를 선택하는 것을 권장합니다.

## 라이선스

이 프로젝트의 소스 코드는 [MIT License](LICENSE)에 따라 공개됩니다.

DNSEver의 서비스, API, 상표, 문서, 기타 DNSEver가 보유한 권리는 이 저장소의 라이선스 적용 대상이 아닙니다.
