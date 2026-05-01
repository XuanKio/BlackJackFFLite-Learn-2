# BlackJack FFLite

Đây là một game mình làm để học Unity.

## Phiên bản Unity

- Unity 2022 LTS

## Ý tưởng gameplay

Game theo luật BlackJack nhưng thêm một số cơ chế để học design pattern tốt hơn.

Số lượng các lá cùng chất trên tay sẽ cộng dồn hiệu ứng:

- Bích: +1 block.
- Cơ: hồi 1 HP.
- Rô: +1 gold.
- Nhép: +1 damage khi gây sát thương.

## Luật cơ bản

- Mỗi round, player có quyền `Hit` hoặc `Stand`; enemy cũng tương tự.
- Mục tiêu là rút bài sao cho tổng điểm `<= 21`.
- Nếu tổng điểm vượt quá 21 thì bị bust.
- Khi so điểm, ai cao hơn sẽ gây sát thương.
- Nếu bằng điểm thì hòa.
- Máu của ai về 0 trước thì người đó thua.

## Buff/Debuff phụ

Nếu player hoặc enemy thua lượt đó, sẽ tính xem chất nào trên tay có số điểm cao nhất để gây buff/debuff cho turn sau:

- Bích cao nhất: gây debuff Silent, nghĩa là sẽ không cộng dồn hiệu ứng lá bài.
- Cơ cao nhất: gây debuff Slow, nghĩa là enemy sẽ đi trước.
- Rô cao nhất: gây buff Critical, nghĩa là khi thắng và tấn công sẽ tạo ra đòn chí mạng. Đòn chí mạng gây thêm 3 sát thương.
- Nhép cao nhất: gây buff Luck, nghĩa là sẽ may mắn hơn khi bốc bài, tránh bị bust và bài sẽ luôn ra được sao cho tổng điểm `>= 18`.
