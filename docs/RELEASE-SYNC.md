# Release 同期（Win Setup / Mac DMG）

**正:** [Keirohi.Avalonia/Keirohi.Avalonia.csproj](../Keirohi.Avalonia/Keirohi.Avalonia.csproj) の `<Version>` = GitHub [keirohi-avalonia](https://github.com/honi0907/keirohi-avalonia) の tag `vX.Y.Z`  
**ソース:** [honi0907/keirohi](https://github.com/honi0907/keirohi) の `main`（遅れている側は先に `git pull`）

| Version | main push | Win Setup | Mac DMG | メモ |
|---------|-----------|-----------|---------|------|
| 1.1.5   | 済        | 済        | 未      | 通経アイコン・通勤経路費・WinUI 凍結・bat 整理 |
| 1.1.4   | 済        | 済        | 済      | Mac 側機能（履歴・外観等） |

詳細な変更内容は GitHub Release の notes と `git log` を参照。WinUI 凍結メモは [avalonia-mac-changes-for-winui.md](./avalonia-mac-changes-for-winui.md)。

## 次にやること

- [ ] **Mac:** `git pull` → `./scripts/build-publish-avalonia-macos.sh` → `bundle` → `create-avalonia-macos-dmg.sh` → `./scripts/upload-avalonia-macos-release.sh`（tag **v1.1.5** に DMG を追加）
- [ ] 上記完了後、この表の 1.1.5 の **Mac DMG** を「済」に更新

## ルール（3 行）

1. **先行** = `main` に push 済み & csproj `Version` が新しい（Release タグと一致）。
2. **遅れ側** = `git pull` → 同じ Version でビルド → 同じ tag に Setup または DMG を載せる。
3. **Version を上げるのは Release を出す側だけ**（もう一方は pull して追従）。

## Cursor に渡す文言

**遅れている側（Win / Mac どちらでも）**

> `honi0907/keirohi` の main と csproj Version を確認し、先行している Release（Win / Mac の vX.Y.Z）に合わせて pull・ビルド・不足している Setup/DMG を同じ tag に載せて。[docs/RELEASE-SYNC.md](./RELEASE-SYNC.md) の表も更新して。

**Windows で改良して Release まで**

> 変更を commit & push して Version を上げ、Setup Release。RELEASE-SYNC に Win 完了を書いて。

**Mac だけ先行したあと**

> main の Version X.Y.Z に合わせて Windows Setup が無ければ Release に upload。RELEASE-SYNC を更新。
