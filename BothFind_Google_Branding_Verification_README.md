# BothFind — Google Branding Verification dəyişiklikləri

Bu paket `RasimAliyew2/GloryLikeWebApp` repozitoriyasının `506d3b9` commit-i əsasında hazırlanıb.

## Paketdə edilən əsas dəyişikliklər

- `https://bothfind.com/` üçün login tələb etməyən public landing page yaradılıb.
- Landing page BothFind-i skills-first recruitment platforması kimi aydın təqdim edir.
- Ana səhifədə görünən link kimi tam `https://bothfind.com/privacy` ünvanı istifadə edilib.
- `https://bothfind.com/privacy` login olmadan açılır və Google user data üçün Access, Use, Storage və Sharing açıqlamalarını ehtiva edir.
- `https://bothfind.com/terms` public Terms of Service səhifəsi əlavə edilib.
- Login, Registration və daxili səhifələrdə görünən `SkillMatch` brendi `BothFind` ilə uyğunlaşdırılıb.
- Registration səhifəsində Google/Apple düymələri real social login route-larına qoşulub.
- Desktop və mobil ekranlar üçün responsive dizayn əlavə edilib.

## Deploy-dan sonra Google Cloud-da istifadə ediləcək ünvanlar

- Application home page: `https://bothfind.com/`
- Application privacy policy: `https://bothfind.com/privacy`
- Application terms of service: `https://bothfind.com/terms`
- Authorized domain: `bothfind.com`

Google Cloud OAuth consent screen-də Privacy Policy URL məhz ana səhifədəki linklə eyni olmalıdır.

## Deploy-dan sonra yoxlama

1. Incognito pəncərədə `https://bothfind.com/` açılmalıdır və login səhifəsinə yönləndirməməlidir.
2. Ana səhifənin header, privacy block və footer hissəsində Privacy Policy linki görünməlidir.
3. `https://bothfind.com/privacy` və `https://bothfind.com/terms` hesab olmadan açılmalıdır.
4. `https://bothfind.com/SignIn` səhifəsində brend adı `BothFind` görünməlidir.
5. Google düyməsi OAuth axınını başlatmalıdır.

Rəsmi tələblər:

- https://support.google.com/cloud/answer/13464321
- https://support.google.com/cloud/answer/13806988
