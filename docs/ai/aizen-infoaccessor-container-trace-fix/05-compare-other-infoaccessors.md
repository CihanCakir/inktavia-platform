# 05 - Compare Other InfoAccessors

Inspect server/channel/accessor patterns that already work.

Search:

```text
IAizenServerInfoAccessor
IAizenChannelInfoAccessor
ServerInfo
ChannelInfo
AizenInfoMiddleware
```

## Questions

- How are server/channel infos set?
- Do they use `IAizenInfoContainer`?
- Are they visible inside handlers?
- Are their accessors using the same DI lifetime as user accessor?
- Do they read from config, container, HttpContext, static, or AsyncLocal?
- What pattern should user info follow?

## Output Required

```md
# Other Accessor Comparison

## ServerInfo Pattern
- ...

## ChannelInfo Pattern
- ...

## UserInfo Pattern
- ...

## Difference Causing Problem
- ...

## Pattern To Reuse
- ...
```
