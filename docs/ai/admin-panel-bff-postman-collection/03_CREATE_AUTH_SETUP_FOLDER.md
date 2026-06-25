# 03 — Create Auth Setup Folder

Create folder `00 - Auth Setup`.

Add requests for:

- Login with Username
- Login with Phone
- Send OTP
- Check OTP
- Login with OTP
- Refresh Identity Token
- Change Password

Login/refresh responses must capture `identityAccessToken`, `identityRefreshToken`, and `X_Aizen_User_Token` where possible.

Only `Change Password` should send `X-Aizen-User-Token`.
