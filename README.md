# BlackJack FFLite

BlackJack FFLite là prototype card game làm bằng Unity để thử nghiệm gameplay kiểu Blackjack kết hợp stack hiệu ứng theo chất bài, status theo round, và UI card-game có deck ở giữa bàn.

Project hiện ưu tiên code dễ đọc, tách domain/gameplay/UI vừa đủ để tiếp tục polish.

## Unity

- Unity: `2022.3.62f3` hoặc Unity 2022 LTS tương đương.
- Scene chính: `Assets/Scenes/SampleScene.unity`.
- Script runtime chính: `Assets/Project/Scripts/Bootstrap/GameInstaller.cs`.

## Cách chơi

- Mỗi round chia 2 lá cho Player và 2 lá cho Enemy.
- Enemy card ban đầu úp, chỉ lật khi Enemy bắt đầu hành động hoặc khi resolve round.
- Click vào bộ bài ở giữa bàn để `HIT`.
- Bấm `STAND` để khóa điểm Player, cho Enemy hành động và resolve round.
- Sau khi round xong, bấm `NEXT` để sang round mới.
- Khi một nhân vật còn `0 HP`, game kết thúc và dùng `RESTART` để chơi lại.

Luật tính điểm giống Blackjack:

- Mục tiêu là tổng điểm `<= 21`.
- Quá `21` là bust.
- `A` tính là `11`, nhưng tự giảm thành `1` nếu cần để tránh bust.
- `J`, `Q`, `K` tính là `10`.
- Ai có điểm cao hơn sẽ thắng round và gây damage.
- Nếu bằng điểm thì hòa, không gây damage.

Enemy AI hiện tại hit đến khi đạt ít nhất `17`, rồi stand.

## Hiệu ứng theo chất bài

Khi resolve round, mỗi chất bài trên tay tạo stack hiệu ứng theo số lượng lá cùng chất. Nếu nhân vật đang bị `Silent`, các hiệu ứng theo chất không kích hoạt.

| Chất | UI label | Hiệu ứng |
| --- | --- | --- |
| Spade / Bích | `Block` | Mỗi lá cho `+1 Block`. Block giảm damage nhận vào trước HP. |
| Heart / Cơ | `Heal` | Mỗi lá hồi `1 HP`. |
| Diamond / Rô | `Gold` | Mỗi lá cho `+1 Gold`. |
| Club / Nhép | `Damage` | Mỗi lá cho `+1 bonus damage` khi nhân vật đó gây damage. |

Các icon effect có tooltip: hover chuột vào icon để xem tên, số stack và mô tả tác dụng.

## Status sau round

Sau khi có người thắng round, hệ thống lấy tay bài của người thua và tìm chất có tổng điểm cao nhất. Chất mạnh nhất đó tạo status cho round kế tiếp:

| Chất mạnh nhất của người thua | Status tạo ra | Target | Tác dụng |
| --- | --- | --- | --- |
| Spade / Bích | `Silent` | Người thắng | Tắt hiệu ứng theo chất trong round kế tiếp. |
| Heart / Cơ | `Slow` | Người thắng | Bị chậm, Enemy/đối thủ hành động trước trong round kế tiếp. |
| Diamond / Rô | `Critical` | Người thua | Khi thắng round kế tiếp, gây thêm `+3 damage`. |
| Club / Nhép | `Luck` | Người thua | Lần draw kế tiếp ưu tiên lá không bust và cố đưa điểm lên `>= 18`. |

Status hiện có duration `1` round. Nếu add lại cùng status, status cũ bị thay bằng status mới.

## UI hiện tại

- Deck nằm giữa bàn, xếp thẳng, là button `HIT`.
- Chỉ còn nút action riêng là `STAND`; `HIT` rời cũ bị runtime ẩn nếu vẫn tồn tại trong scene.
- Effect của Player xếp ở bên trái, Enemy xếp ở bên phải.
- Enemy info nằm góc phải phía trên, score ẩn bằng `?` đến khi reveal.
- Round result hiện bằng banner ngắn ở giữa.
- Card được deal từ deck ra hand bằng animation, sau đó mới flip bằng shader `Assets/Resources/Shaders/CardRevealUI.shader`.

## Lưu ý khi chỉnh layout trong Unity

`GameInstaller` không còn ép layout cố định cho các object đã có trong scene. Nghĩa là nếu bạn kéo `Deck Count`, hand roots, info text, button, effect roots trong Scene view thì vị trí đó sẽ được giữ khi Play.

Runtime chỉ tự tạo và đặt vị trí mặc định cho object bị thiếu reference, ví dụ:

- `Deck Pile` nếu `deckPileRoot` chưa được gán.
- `Deck Count` nếu `deckCountText` chưa được gán.
- Tooltip hoặc round banner nếu chưa có object tương ứng.

Nếu kéo UI mà không lưu:

- Đảm bảo bạn chỉnh ở Edit Mode, không phải Play Mode.
- Save scene sau khi chỉnh.
- Kiểm tra parent có `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `GridLayoutGroup`, hoặc `ContentSizeFitter` không; các component này có thể tự điều khiển vị trí con.

## Cấu trúc code

```text
Assets/Project/Scripts
  Bootstrap/
    GameInstaller.cs          Runtime composition, UI binding, coroutines, animation.
    GameContext.cs            Context dùng cho state-machine path.

  Domain/
    Cards/                    Card, rank, suit, deck interface, standard deck.
    Characters/               Character HP, block, gold, statuses, hand.
    Hands/                    Hand scoring and suit helpers.
    Statuses/                 Silent, Slow, Critical, Luck.

  Gameplay/
    AI/                       Enemy decision strategy.
    Combat/                   Damage calculation and round resolution.
    Drawing/                  Normal and lucky draw policies.
    Effects/                  Suit effects and resolver.
    Rules/                    Strongest suit and after-round status rules.

  Core/
    Events/                   Lightweight event structs/bus.
    StateMachine/             State-machine abstractions.

  States/                     Experimental/legacy state flow classes.
```

Hiện gameplay chạy trực tiếp qua `GameInstaller`. Các lớp state-machine vẫn còn trong project để tiếp tục tách flow sau này.

## Build / kiểm tra nhanh

Trong root project có thể chạy:

```powershell
dotnet build "Assembly-CSharp.csproj" --no-restore
```

Nếu `Temp/obj/.../project.assets.json` bị thiếu, chạy build có restore một lần:

```powershell
dotnet build "Assembly-CSharp.csproj"
```

Unity vẫn là nguồn verify chính cho scene/UI vì project dùng `UnityEngine.UI`, animation coroutine và asset references.

## Asset chính

- Card sprites: `Assets/Project/Assets/card.png`
- Effect icons: `Assets/Project/Assets/effectIcon.png`
- Font: `Assets/Project/Font/Kenney Future.ttf`
- UI shader flip/reveal: `Assets/Resources/Shaders/CardRevealUI.shader`

## Ghi chú phát triển

- Giữ domain logic tách khỏi UI càng nhiều càng tốt.
- Nếu thêm effect/status mới, cập nhật cả domain rule, UI icon, tooltip và README.
- Khi polish UI, ưu tiên chỉnh vị trí trong Scene thay vì hardcode runtime layout.
- Tránh thêm layout pass ép `RectTransform` trong `GameInstaller`, trừ object fallback được tạo lúc thiếu reference.
