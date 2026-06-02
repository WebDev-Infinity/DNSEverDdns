# DNSEver DDNS Windows Client

DNSEver에서 Windows용 다이나믹 DNS 클라이언트 지원이 중단된 상황을 보완하기 위한 C# 기반 Windows 클라이언트입니다.

## 구성

- `DNSEverDdns.Core`: DNSEver 다이나믹 DNS API 호출과 XML 응답 파싱을 담당합니다.
- `DNSEverDdns.Win`: Windows Forms 기반 트레이 앱입니다.

## 주요 기능

- DNSEver DDNS 인증 코드 기반 Basic 인증
- 등록 호스트 목록 조회
- 지정 호스트 DDNS 업데이트
- 지정 IP 업데이트 또는 DNSEver 서버 감지 IP 사용
- 트레이 아이콘 상주
- 주기 업데이트
- Windows DPAPI 기반 인증 코드 보호 저장
- 어두운 테마와 밝은 테마 선택
- `%LOCALAPPDATA%\DNSEverDdns\logs` 로그 기록

## 빌드

```powershell
dotnet build .\DNSEverDdns.slnx
```

## 실행

```powershell
dotnet run --project .\DNSEverDdns.Win\DNSEverDdns.Win.csproj
```

처음 실행하면 설정 화면이 표시됩니다.

1. DNSEver 사용자 아이디를 입력합니다.
2. DNSEver DDNS 인증 코드를 입력합니다.
3. `호스트 조회` 버튼으로 등록된 호스트를 가져옵니다.
4. 업데이트할 호스트만 남깁니다.
5. 업데이트 주기를 설정하고 저장합니다.

## 배포 예시

```powershell
dotnet publish .\DNSEverDdns.Win\DNSEverDdns.Win.csproj -c Release -r win-x64 --self-contained false
```

## 참고

DNSEver 문서 기준으로 너무 잦은 업데이트는 `320: Too Many updates sent` 응답을 받을 수 있으므로 업데이트 주기는 최소 1분 이상으로 제한했습니다.
