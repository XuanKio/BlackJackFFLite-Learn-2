using System.Collections;
using System.Collections.Generic;
using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Domain.Statuses;
using BlackJackFFLite.Gameplay.AI;
using BlackJackFFLite.Gameplay.Combat;
using BlackJackFFLite.Gameplay.Drawing;
using BlackJackFFLite.Gameplay.Effects;
using BlackJackFFLite.Gameplay.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlackJackFFLite.Bootstrap
{
    public sealed class GameInstaller : MonoBehaviour
    {
        private const int PlayerMaxHp = 30;
        private const int EnemyMaxHp = 30;
        private const int BaseDamage = 3;
        private const float DealDuration = 0.32f;
        private const float FlipDuration = 0.24f;
        private const float DealPause = 0.08f;
        private const string CardRevealShaderPath = "Shaders/CardRevealUI";

        [Header("Texts")]
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Text enemyInfoText;
        [SerializeField] private Text playerScoreText;
        [SerializeField] private Text enemyScoreText;
        [SerializeField] private Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button hitButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button standButton;
        [SerializeField] private Button nextRoundButton;
        [SerializeField] private Button restartButton;

        [Header("Layout")]
        [SerializeField] private Transform playerHandRoot;
        [SerializeField] private Transform enemyHandRoot;
        [SerializeField] private Transform playerEffectRoot;
        [SerializeField] private Transform enemyEffectRoot;

        [Header("Board")]
        [SerializeField] private RectTransform deckPileRoot;
        [SerializeField] private Text deckCountText;
        [SerializeField] private Text actionInfoText;
        [SerializeField] private CanvasGroup roundBannerGroup;
        [SerializeField] private Text roundBannerText;
        [SerializeField] private Font uiFont;
        [SerializeField] private Sprite panelSprite;

        [Header("Cards")]
        [SerializeField] private Sprite[] heartCards;
        [SerializeField] private Sprite[] diamondCards;
        [SerializeField] private Sprite[] clubCards;
        [SerializeField] private Sprite[] spadeCards;
        [SerializeField] private Sprite cardBack;

        [Header("Effect Icons")]
        [SerializeField] private Sprite blockIcon;
        [SerializeField] private Sprite damageIcon;
        [SerializeField] private Sprite luckIcon;
        [SerializeField] private Sprite slowIcon;
        [SerializeField] private Sprite criticalIcon;
        [SerializeField] private Sprite silentIcon;
        [SerializeField] private Sprite regenIcon;

        private readonly Dictionary<string, int> _effectAmounts = new Dictionary<string, int>();

        private IDeck _deck;
        private Character _player;
        private Character _enemy;
        private CardDrawService _drawService;
        private RoundResolver _roundResolver;
        private IEnemyDecisionStrategy _enemyAI;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Transform _animationLayer;
        private RectTransform _effectTooltipRoot;
        private CanvasGroup _effectTooltipGroup;
        private Text _effectTooltipTitleText;
        private Text _effectTooltipBodyText;
        private Shader _cardRevealShader;
        private Coroutine _activeRoutine;
        private bool _roundFinished;
        private bool _enemyActedThisRound;
        private bool _inputLocked;

        private void Awake()
        {
            CacheCanvas();
            EnsureRuntimeBoardWidgets();

            if (deckButton != null)
                deckButton.onClick.AddListener(OnHitClicked);

            if (standButton != null)
                standButton.onClick.AddListener(OnStandClicked);

            if (nextRoundButton != null)
                nextRoundButton.onClick.AddListener(StartRound);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
        }

        private void Start()
        {
            CreateGameServices();
            RestartGame();
        }

        private void CreateGameServices()
        {
            _deck = new StandardDeck();
            _player = new Character("Player", PlayerMaxHp, BaseDamage);
            _enemy = new Character("Enemy", EnemyMaxHp, BaseDamage);

            _drawService = new CardDrawService(new NormalDrawPolicy(), new LuckyDrawPolicy());
            _enemyAI = new BasicEnemyAI();

            SuitEffectResolver suitEffectResolver = new SuitEffectResolver(new ISuitEffect[]
            {
                new SpadeEffect(),
                new HeartEffect(),
                new DiamondEffect(),
                new ClubEffect()
            });

            AfterRoundStatusResolver statusResolver = new AfterRoundStatusResolver(new StrongestSuitResolver());
            _roundResolver = new RoundResolver(new DamageCalculator(), suitEffectResolver, statusResolver);
        }

        private void RestartGame()
        {
            if (_player == null || _enemy == null || _deck == null)
                return;

            _player.ResetForNewGame();
            _enemy.ResetForNewGame();
            _deck.Reset();
            _effectAmounts.Clear();
            StartExclusive(StartRoundSequence());
        }

        private void StartRound()
        {
            if (_player == null || _enemy == null || _player.IsDead || _enemy.IsDead)
                return;

            if (_inputLocked)
                return;

            StartExclusive(StartRoundSequence());
        }

        private IEnumerator StartRoundSequence()
        {
            _inputLocked = true;
            _roundFinished = false;
            _enemyActedThisRound = false;
            _effectAmounts.Clear();

            SetRoundButtons(false);
            SetObjectActive(nextRoundButton, false);
            SetObjectActive(restartButton, false);
            HideRoundBanner();

            _player.ResetForNewRound();
            _enemy.ResetForNewRound();

            SetMessage("Dealing from the deck...");
            UpdateView(showEnemyCards: false);
            UpdateDeckCount();

            yield return DealStartingHandsSequence();

            if (_player.Statuses.Has<SlowStatus>())
            {
                SetMessage("Slow triggers. Enemy acts first.");
                yield return RunEnemyTurnSequence();
                SetMessage("Enemy acted first. Click deck to draw or Stand.");
            }
            else
            {
                SetMessage("Click deck to draw. Stand to resolve.");
            }

            UpdateView(showEnemyCards: _enemyActedThisRound);

            _inputLocked = false;
            SetRoundButtons(true);
            SetObjectActive(nextRoundButton, false);
        }

        private IEnumerator DealStartingHandsSequence()
        {
            yield return DealCardTo(_player, revealAfterDeal: true, showEnemyCards: false);
            yield return DealCardTo(_enemy, revealAfterDeal: false, showEnemyCards: false);
            yield return DealCardTo(_player, revealAfterDeal: true, showEnemyCards: false);
            yield return DealCardTo(_enemy, revealAfterDeal: false, showEnemyCards: false);
        }

        private void OnHitClicked()
        {
            if (_roundFinished || _inputLocked)
                return;

            StartExclusive(PlayerHitSequence());
        }

        private IEnumerator PlayerHitSequence()
        {
            _inputLocked = true;
            SetRoundButtons(false);
            SetMessage("Player draws.");

            yield return DealCardTo(_player, revealAfterDeal: true, showEnemyCards: _enemyActedThisRound);

            if (_player.Hand.IsBust)
            {
                SetMessage("Player busts. Revealing enemy hand.");
                yield return ResolveRoundSequence();
                yield break;
            }

            SetMessage("Click deck to draw. Stand to resolve.");
            _inputLocked = false;
            SetRoundButtons(true);
        }

        private void OnStandClicked()
        {
            if (_roundFinished || _inputLocked)
                return;

            StartExclusive(PlayerStandSequence());
        }

        private IEnumerator PlayerStandSequence()
        {
            _inputLocked = true;
            SetRoundButtons(false);
            SetMessage("Player stands. Enemy reveals.");

            if (!_enemyActedThisRound)
                yield return RunEnemyTurnSequence();

            yield return ResolveRoundSequence();
        }

        private IEnumerator RunEnemyTurnSequence()
        {
            if (!_enemyActedThisRound)
            {
                yield return RevealEnemyHandSequence();
                _enemyActedThisRound = true;
            }

            while (_enemyAI.Decide(_enemy, _player) == EnemyAction.Hit)
            {
                SetMessage("Enemy hits.");
                yield return DealCardTo(_enemy, revealAfterDeal: true, showEnemyCards: true);

                if (_enemy.Hand.IsBust)
                    break;
            }

            SetMessage(_enemy.Hand.IsBust ? "Enemy busts." : "Enemy stands.");
            yield return new WaitForSeconds(0.18f);
        }

        private IEnumerator ResolveRoundSequence()
        {
            if (!_enemyActedThisRound)
            {
                yield return RevealEnemyHandSequence();
                _enemyActedThisRound = true;
            }

            _roundFinished = true;
            SetRoundButtons(false);
            SetObjectActive(nextRoundButton, false);

            int playerHpBefore = _player.Hp;
            int enemyHpBefore = _enemy.Hp;

            RoundResult result = _roundResolver.Resolve(_player, _enemy);

            SetMessage(BuildRoundMessage(result));
            UpdateView(showEnemyCards: true);

            yield return PlayRoundResolvedPolish(result, playerHpBefore, enemyHpBefore);

            if (_player.IsDead || _enemy.IsDead)
            {
                Character winner = _player.IsDead ? _enemy : _player;
                SetMessage($"{BuildRoundMessage(result)}\n{winner.Id} wins the game.");
                SetObjectActive(nextRoundButton, false);
                SetObjectActive(restartButton, true);
                _inputLocked = false;
                yield break;
            }

            SetObjectActive(nextRoundButton, true);
            SetObjectActive(restartButton, true);
            _inputLocked = false;
        }

        private IEnumerator DealCardTo(Character character, bool revealAfterDeal, bool showEnemyCards)
        {
            Card card = DrawFor(character);
            UpdateDeckCount();

            int pendingCardIndex = character.Hand.CardCount - 1;
            Image placeholder = RenderHands(showEnemyCards, character, pendingCardIndex, pendingVisible: false);

            yield return AnimateCardFromDeckTo(placeholder);

            if (placeholder != null)
            {
                SetGraphicAlpha(placeholder, 1f);
                placeholder.sprite = cardBack;

                if (revealAfterDeal)
                    yield return FlipCard(placeholder, GetCardSprite(card));
                else
                    yield return PopTransform(placeholder.rectTransform, 1.06f, 0.12f);
            }

            UpdateView(showEnemyCards);

            if (revealAfterDeal)
                yield return PulseSuitStack(character, card.Suit);

            yield return new WaitForSeconds(DealPause);
        }

        private IEnumerator RevealEnemyHandSequence()
        {
            if (_enemy.Hand.CardCount == 0)
                yield break;

            SetMessage("Enemy cards flip.");
            RenderHands(showEnemyCards: false, pendingOwner: null, pendingCardIndex: -1, pendingVisible: true);

            for (int i = 0; i < _enemy.Hand.CardCount; i++)
            {
                Image cardImage = GetCardImage(enemyHandRoot, i);

                if (cardImage != null)
                    yield return FlipCard(cardImage, GetCardSprite(_enemy.Hand.Cards[i]));
            }

            UpdateView(showEnemyCards: true);
            yield return new WaitForSeconds(0.12f);
        }

        private IEnumerator PlayRoundResolvedPolish(RoundResult result, int playerHpBefore, int enemyHpBefore)
        {
            Color bannerColor = result.Winner == RoundWinner.Player
                ? new Color(0.98f, 0.77f, 0.28f)
                : result.Winner == RoundWinner.Enemy
                    ? new Color(0.95f, 0.35f, 0.31f)
                    : new Color(0.74f, 0.82f, 0.92f);

            string title = result.IsDraw ? "DRAW" : $"{result.Winner.ToString().ToUpperInvariant()} WINS";
            string subtitle = result.IsDraw ? result.Reason : $"{result.Reason}  |  Damage {result.DamageDealt}";

            yield return ShowRoundBanner(title, subtitle, bannerColor);

            if (playerHpBefore > _player.Hp)
                StartCoroutine(FloatingText(playerInfoText != null ? playerInfoText.rectTransform : null, $"-{playerHpBefore - _player.Hp} HP", new Color(1f, 0.36f, 0.32f)));

            if (enemyHpBefore > _enemy.Hp)
                StartCoroutine(FloatingText(enemyInfoText != null ? enemyInfoText.rectTransform : null, $"-{enemyHpBefore - _enemy.Hp} HP", new Color(1f, 0.36f, 0.32f)));

            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator ShowRoundBanner(string title, string subtitle, Color color)
        {
            if (roundBannerGroup == null || roundBannerText == null)
                yield break;

            RectTransform rect = (RectTransform)roundBannerGroup.transform;
            roundBannerText.text = $"{title}\n{subtitle}";
            roundBannerText.color = color;
            roundBannerGroup.alpha = 0f;
            rect.localScale = new Vector3(0.9f, 0.9f, 1f);
            roundBannerGroup.gameObject.SetActive(true);

            const float fadeIn = 0.16f;
            float elapsed = 0f;

            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeIn);
                float eased = Smooth(t);
                roundBannerGroup.alpha = eased;
                rect.localScale = Vector3.Lerp(new Vector3(0.9f, 0.9f, 1f), Vector3.one, eased);
                yield return null;
            }

            roundBannerGroup.alpha = 1f;
            rect.localScale = Vector3.one;
            yield return new WaitForSeconds(0.58f);
        }

        private IEnumerator FloatingText(RectTransform target, string value, Color color)
        {
            if (_animationLayer == null || target == null)
                yield break;

            GameObject textObject = new GameObject("Floating Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(_animationLayer, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 36);
            Vector2 startPosition = GetCanvasPosition(target);
            rect.anchoredPosition = startPosition;

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = GetFont();
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            const float duration = 0.7f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPosition, startPosition + new Vector2(0f, 54f), Smooth(t));
                Color current = text.color;
                current.a = 1f - t;
                text.color = current;
                yield return null;
            }

            Destroy(textObject);
        }

        private IEnumerator AnimateCardFromDeckTo(Image placeholder)
        {
            if (_animationLayer == null || deckPileRoot == null || placeholder == null)
            {
                if (placeholder != null)
                    SetGraphicAlpha(placeholder, 1f);

                yield break;
            }

            GameObject cardObject = new GameObject("Dealing Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
            cardObject.transform.SetParent(_animationLayer, false);

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(88f, 124f);
            rect.localScale = Vector3.one;

            Image image = cardObject.GetComponent<Image>();
            image.sprite = cardBack;
            image.preserveAspect = true;
            image.color = Color.white;

            Shadow shadow = cardObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(0f, -6f);

            Vector2 start = GetCanvasPosition(deckPileRoot);
            Vector2 end = GetCanvasPosition(placeholder.rectTransform);
            float startRotation = Random.Range(-9f, 9f);
            float endRotation = Random.Range(-3f, 3f);

            float elapsed = 0f;

            while (elapsed < DealDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DealDuration);
                float eased = Smooth(t);
                Vector2 arc = Vector2.up * Mathf.Sin(eased * Mathf.PI) * 38f;
                rect.anchoredPosition = Vector2.Lerp(start, end, eased) + arc;
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startRotation, endRotation, eased));
                rect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.04f, Mathf.Sin(eased * Mathf.PI));
                yield return null;
            }

            Destroy(cardObject);
            SetGraphicAlpha(placeholder, 1f);
        }

        private IEnumerator FlipCard(Image image, Sprite frontSprite)
        {
            if (image == null)
                yield break;

            RectTransform rect = image.rectTransform;
            Vector3 originalScale = rect.localScale;
            Material material = CreateRevealMaterial();

            if (material != null)
                image.material = material;

            float halfDuration = FlipDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                SetRevealMaterial(material, 0.25f + t * 0.35f, Mathf.Lerp(0.15f, 0.8f, t));
                rect.localScale = new Vector3(Mathf.Lerp(originalScale.x, originalScale.x * 0.08f, Smooth(t)), originalScale.y, originalScale.z);
                yield return null;
            }

            image.sprite = frontSprite;
            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                SetRevealMaterial(material, 0.55f + t * 0.45f, Mathf.Lerp(0.8f, 0f, t));
                rect.localScale = new Vector3(Mathf.Lerp(originalScale.x * 0.08f, originalScale.x, Smooth(t)), originalScale.y, originalScale.z);
                yield return null;
            }

            rect.localScale = originalScale;

            if (material != null)
            {
                image.material = null;
                Destroy(material);
            }

            yield return PopTransform(rect, 1.05f, 0.12f);
        }

        private IEnumerator PopTransform(RectTransform rect, float scale, float duration)
        {
            if (rect == null)
                yield break;

            Vector3 original = rect.localScale;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                if (rect == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                rect.localScale = Vector3.Lerp(original, original * scale, Smooth(t));
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < half)
            {
                if (rect == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                rect.localScale = Vector3.Lerp(original * scale, original, Smooth(t));
                yield return null;
            }

            rect.localScale = original;
        }

        private IEnumerator PulseSuitStack(Character character, CardSuit suit)
        {
            Transform root = character == _player ? playerEffectRoot : enemyEffectRoot;
            Transform chip = root != null ? root.Find($"Effect {GetSuitEffectKey(suit)}") : null;

            if (chip == null)
                yield break;

            yield return PopTransform((RectTransform)chip, 1.12f, 0.16f);
        }

        private Card DrawFor(Character character)
        {
            return _drawService.DrawFor(character, _deck);
        }

        private string BuildRoundMessage(RoundResult result)
        {
            if (result.IsDraw)
                return $"Draw: {result.Reason}.";

            return $"{result.Winner} wins: {result.Reason}. Damage: {result.DamageDealt}.";
        }

        private void SetRoundButtons(bool active)
        {
            bool enabled = active && !_roundFinished && !_inputLocked;

            if (hitButton != null)
                hitButton.gameObject.SetActive(false);

            if (deckButton != null)
                deckButton.interactable = enabled;

            if (standButton != null)
                standButton.interactable = enabled;
        }

        private void UpdateView(bool showEnemyCards)
        {
            if (_player == null || _enemy == null)
                return;

            HideEffectTooltip();

            if (playerInfoText != null)
                playerInfoText.text = BuildCharacterInfo(_player);

            if (enemyInfoText != null)
                enemyInfoText.text = BuildCharacterInfo(_enemy);

            if (playerScoreText != null)
                playerScoreText.text = $"Score: {_player.Hand.Score}";

            if (enemyScoreText != null)
                enemyScoreText.text = showEnemyCards ? $"Score: {_enemy.Hand.Score}" : "Score: ?";

            RenderHands(showEnemyCards, pendingOwner: null, pendingCardIndex: -1, pendingVisible: true);
            RenderEffects(playerEffectRoot, _player, revealHandStacks: true);
            RenderEffects(enemyEffectRoot, _enemy, revealHandStacks: showEnemyCards);
            UpdateDeckCount();
        }

        private Image RenderHands(bool showEnemyCards, Character pendingOwner, int pendingCardIndex, bool pendingVisible)
        {
            Image playerPending = RenderHand(playerHandRoot, _player.Hand.Cards, revealCards: true, pendingOwner == _player ? pendingCardIndex : -1, pendingVisible);
            Image enemyPending = RenderHand(enemyHandRoot, _enemy.Hand.Cards, revealCards: showEnemyCards, pendingOwner == _enemy ? pendingCardIndex : -1, pendingVisible);

            if (pendingOwner == _player)
                return playerPending;

            if (pendingOwner == _enemy)
                return enemyPending;

            return null;
        }

        private string BuildCharacterInfo(Character character)
        {
            return $"{character.Id} | HP {character.Hp}/{character.MaxHp} | Block {character.Block} | Gold {character.Gold}";
        }

        private Image RenderHand(Transform root, IReadOnlyList<Card> cards, bool revealCards, int pendingCardIndex, bool pendingVisible)
        {
            if (root == null)
                return null;

            ClearChildren(root);
            Image pendingImage = null;

            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                bool isPending = i == pendingCardIndex;

                GameObject cardObject = new GameObject($"Card {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow), typeof(LayoutElement));
                cardObject.transform.SetParent(root, false);

                RectTransform rectTransform = (RectTransform)cardObject.transform;
                rectTransform.sizeDelta = new Vector2(88f, 124f);

                LayoutElement layout = cardObject.GetComponent<LayoutElement>();
                layout.preferredWidth = 88f;
                layout.preferredHeight = 124f;

                Shadow shadow = cardObject.GetComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
                shadow.effectDistance = new Vector2(0f, -5f);

                Image image = cardObject.GetComponent<Image>();
                image.sprite = isPending ? cardBack : revealCards ? GetCardSprite(card) : cardBack;
                image.preserveAspect = true;
                image.color = Color.white;

                if (isPending)
                {
                    pendingImage = image;

                    if (!pendingVisible)
                        SetGraphicAlpha(image, 0f);
                }
            }

            if (root is RectTransform rootRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

            return pendingImage;
        }

        private Image GetCardImage(Transform root, int index)
        {
            if (root == null)
                return null;

            Transform card = root.Find($"Card {index}");
            return card != null ? card.GetComponent<Image>() : null;
        }

        private void RenderEffects(Transform root, Character character, bool revealHandStacks)
        {
            if (root == null || character == null)
                return;

            ClearChildren(root);

            bool silent = character.Statuses.Has<SilentStatus>();

            if (revealHandStacks && !silent)
            {
                AddEffectChip(root, blockIcon, GetVisibleStackAmount(character, CardSuit.Spade), GetSuitEffectKey(CardSuit.Spade), "Block");
                AddEffectChip(root, regenIcon, GetVisibleStackAmount(character, CardSuit.Heart), GetSuitEffectKey(CardSuit.Heart), "Heal");
                AddEffectChip(root, luckIcon, GetVisibleStackAmount(character, CardSuit.Diamond), GetSuitEffectKey(CardSuit.Diamond), "Gold");
                AddEffectChip(root, damageIcon, GetVisibleStackAmount(character, CardSuit.Club), GetSuitEffectKey(CardSuit.Club), "Damage");
            }

            if (character.Block > 0 && (!revealHandStacks || silent))
                AddEffectChip(root, blockIcon, character.Block, "StoredBlock", "Block");

            AddEffectChip(root, silentIcon, character.Statuses.Has<SilentStatus>() ? 1 : 0, "Silent", "Silent");
            AddEffectChip(root, slowIcon, character.Statuses.Has<SlowStatus>() ? 1 : 0, "Slow", "Slow");
            AddEffectChip(root, criticalIcon, character.Statuses.Has<CriticalStatus>() ? 1 : 0, "Critical", "Critical");
            AddEffectChip(root, luckIcon, character.Statuses.Has<LuckStatus>() ? 1 : 0, "Luck", "Luck");
        }

        private int GetVisibleStackAmount(Character character, CardSuit suit)
        {
            if (suit == CardSuit.Spade && character.Block > 0)
                return character.Block;

            return character.Hand.CountSuit(suit);
        }

        private void AddEffectChip(Transform root, Sprite sprite, int amount, string key, string label)
        {
            if (sprite == null || amount <= 0)
                return;

            string cacheKey = $"{root.GetInstanceID()}:{key}";
            bool increased = _effectAmounts.TryGetValue(cacheKey, out int previous) && amount > previous;
            _effectAmounts[cacheKey] = amount;

            GameObject chipObject = new GameObject($"Effect {key}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Shadow));
            chipObject.transform.SetParent(root, false);

            RectTransform rect = chipObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(58f, 42f);

            LayoutElement chipLayout = chipObject.GetComponent<LayoutElement>();
            chipLayout.preferredWidth = 58f;
            chipLayout.preferredHeight = 42f;

            Image background = chipObject.GetComponent<Image>();
            background.sprite = panelSprite;
            background.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0.08f, 0.065f, 0.09f, 0.9f);
            background.raycastTarget = true;

            Shadow chipShadow = chipObject.GetComponent<Shadow>();
            chipShadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            chipShadow.effectDistance = new Vector2(0f, -3f);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(chipObject.transform, false);

            RectTransform iconRect = (RectTransform)iconObject.transform;
            SetRect(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7f, 1f), new Vector2(28f, 28f));

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            GameObject amountObject = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            amountObject.transform.SetParent(chipObject.transform, false);

            RectTransform amountRect = (RectTransform)amountObject.transform;
            SetRect(amountRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-15f, 12f), new Vector2(28f, 20f));

            Text amountText = amountObject.GetComponent<Text>();
            amountText.text = $"x{amount}";
            amountText.font = GetFont();
            amountText.fontSize = 13;
            amountText.resizeTextForBestFit = true;
            amountText.resizeTextMinSize = 9;
            amountText.resizeTextMaxSize = 13;
            amountText.alignment = TextAnchor.MiddleCenter;
            amountText.color = new Color(1f, 0.94f, 0.76f);
            amountText.raycastTarget = false;

            Shadow amountShadow = amountObject.GetComponent<Shadow>();
            amountShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            amountShadow.effectDistance = new Vector2(1f, -1f);

            if (increased)
                StartCoroutine(PopTransform(rect, 1.12f, 0.16f));

            AttachEffectTooltip(chipObject, rect, label, amount, key);
        }

        private void AttachEffectTooltip(GameObject chipObject, RectTransform chipRect, string label, int amount, string key)
        {
            EventTrigger trigger = chipObject.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = chipObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowEffectTooltip(chipRect, label, amount, key));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideEffectTooltip());
            trigger.triggers.Add(exit);
        }

        private void ShowEffectTooltip(RectTransform source, string label, int amount, string key)
        {
            EnsureEffectTooltip();

            if (_effectTooltipGroup == null || _effectTooltipTitleText == null || _effectTooltipBodyText == null)
                return;

            _effectTooltipTitleText.text = $"{label} x{amount}";
            _effectTooltipBodyText.text = GetEffectTooltipBody(key, label);
            _effectTooltipGroup.alpha = 1f;
            _effectTooltipGroup.blocksRaycasts = false;
            _effectTooltipGroup.gameObject.SetActive(true);
            PositionEffectTooltip(source);
        }

        private void HideEffectTooltip()
        {
            if (_effectTooltipGroup == null)
                return;

            _effectTooltipGroup.alpha = 0f;
            _effectTooltipGroup.gameObject.SetActive(false);
        }

        private void PositionEffectTooltip(RectTransform source)
        {
            if (_effectTooltipRoot == null || source == null || _canvasRect == null)
                return;

            Vector2 sourcePosition = GetCanvasPosition(source);
            float direction = sourcePosition.x > 0f ? -1f : 1f;
            Vector2 desired = sourcePosition + new Vector2(direction * 126f, 44f);
            Vector2 halfCanvas = _canvasRect.rect.size * 0.5f;
            Vector2 halfTooltip = _effectTooltipRoot.sizeDelta * 0.5f;

            desired.x = Mathf.Clamp(desired.x, -halfCanvas.x + halfTooltip.x + 16f, halfCanvas.x - halfTooltip.x - 16f);
            desired.y = Mathf.Clamp(desired.y, -halfCanvas.y + halfTooltip.y + 16f, halfCanvas.y - halfTooltip.y - 16f);

            _effectTooltipRoot.anchoredPosition = desired;
        }

        private string GetEffectTooltipBody(string key, string label)
        {
            switch (key)
            {
                case "Block":
                case "StoredBlock":
                    return "Spade stack. Reduces incoming damage this round before HP is lost.";
                case "Heal":
                    return "Heart stack. Restores HP when the round resolves.";
                case "Gold":
                    return "Diamond stack. Adds gold after resolution and can create Critical pressure.";
                case "Damage":
                    return "Club stack. Adds bonus damage to the winner's attack this round.";
                case "Silent":
                    return "Status. Suit stacks are muted while this is active.";
                case "Slow":
                    return "Status. This character acts after the opponent next round.";
                case "Critical":
                    return "Status. Adds bonus damage when this character wins a round.";
                case "Luck":
                    return "Status. Improves the next draw by avoiding bad bust cards when possible.";
                default:
                    return $"{label} effect. Hover icons to inspect active stacks and statuses.";
            }
        }

        private Sprite GetCardSprite(Card card)
        {
            Sprite[] sprites = GetSuitSprites(card.Suit);
            int index = (int)card.Rank;

            if (sprites == null || index < 0 || index >= sprites.Length)
                return cardBack;

            return sprites[index];
        }

        private Sprite[] GetSuitSprites(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Heart:
                    return heartCards;
                case CardSuit.Diamond:
                    return diamondCards;
                case CardSuit.Club:
                    return clubCards;
                case CardSuit.Spade:
                    return spadeCards;
                default:
                    return null;
            }
        }

        private string GetSuitEffectKey(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Spade:
                    return "Block";
                case CardSuit.Heart:
                    return "Heal";
                case CardSuit.Diamond:
                    return "Gold";
                case CardSuit.Club:
                    return "Damage";
                default:
                    return suit.ToString();
            }
        }

        private void CacheCanvas()
        {
            _canvas = FindObjectOfType<Canvas>();

            if (_canvas == null)
                return;

            _canvasRect = (RectTransform)_canvas.transform;

            GameObject animationObject = new GameObject("Card Animation Layer", typeof(RectTransform));
            animationObject.transform.SetParent(_canvas.transform, false);
            _animationLayer = animationObject.transform;

            RectTransform animationRect = (RectTransform)_animationLayer;
            animationRect.anchorMin = Vector2.zero;
            animationRect.anchorMax = Vector2.one;
            animationRect.offsetMin = Vector2.zero;
            animationRect.offsetMax = Vector2.zero;
            animationRect.SetAsLastSibling();
        }

        private void EnsureRuntimeBoardWidgets()
        {
            if (_canvas == null)
                return;

            HideLegacyBoardObjects();

            if (uiFont == null)
                uiFont = ResolveExistingFont();

            if (panelSprite == null)
                panelSprite = ResolvePanelSprite();

            bool createdDeckPile = false;

            if (deckPileRoot == null)
            {
                deckPileRoot = CreateRuntimeDeckPile();
                createdDeckPile = true;
            }

            EnsureDeckHitTarget();

            if (createdDeckPile)
                NormalizeDeckPileVisuals();

            if (deckCountText == null)
            {
                deckCountText = CreateRuntimeText("Deck Count", _canvas.transform, "52 cards", 15, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.7f));
                SetRect(deckCountText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(180f, 28f));
            }

            HideActionInfoPanel();

            if (roundBannerGroup == null || roundBannerText == null)
                CreateRuntimeRoundBanner();

            EnsureEffectTooltip();

            if (_animationLayer != null)
                _animationLayer.SetAsLastSibling();

            if (_effectTooltipRoot != null)
                _effectTooltipRoot.SetAsLastSibling();
        }

        private RectTransform CreateRuntimeDeckPile()
        {
            GameObject deckObject = new GameObject("Deck Pile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            deckObject.transform.SetParent(_canvas.transform, false);

            RectTransform deckRect = deckObject.GetComponent<RectTransform>();
            SetRect(deckRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(128f, 158f));

            Image hitTarget = deckObject.GetComponent<Image>();
            hitTarget.color = new Color(1f, 1f, 1f, 0f);
            hitTarget.raycastTarget = true;

            Button button = deckObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hitTarget;

            for (int i = 0; i < 4; i++)
            {
                Image card = CreateRuntimeImage($"Deck Card {i}", deckObject.transform, cardBack, Color.white);
                SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -i * 3f), new Vector2(88f, 124f));
                card.preserveAspect = true;
            }

            Text label = CreateRuntimeText("Deck Label", deckObject.transform, "HIT", 16, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.62f));
            SetRect(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(128f, 24f));

            return deckRect;
        }

        private void EnsureDeckHitTarget()
        {
            if (deckPileRoot == null)
                return;

            Image hitTarget = deckPileRoot.GetComponent<Image>();

            if (hitTarget == null)
                hitTarget = deckPileRoot.gameObject.AddComponent<Image>();

            hitTarget.color = new Color(1f, 1f, 1f, 0f);
            hitTarget.raycastTarget = true;

            deckButton = deckPileRoot.GetComponent<Button>();

            if (deckButton == null)
                deckButton = deckPileRoot.gameObject.AddComponent<Button>();

            deckButton.transition = Selectable.Transition.None;
            deckButton.targetGraphic = hitTarget;
        }

        private void NormalizeDeckPileVisuals()
        {
            if (deckPileRoot == null)
                return;

            for (int i = 0; i < deckPileRoot.childCount; i++)
            {
                Transform child = deckPileRoot.GetChild(i);

                if (child.name.StartsWith("Deck Card"))
                {
                    int cardIndex = ExtractTrailingIndex(child.name);
                    RectTransform cardRect = child as RectTransform;
                    SetRect(cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -cardIndex * 3f), new Vector2(88f, 124f));
                    child.localEulerAngles = Vector3.zero;
                }
                else if (child.name == "Deck Label")
                {
                    Text label = child.GetComponent<Text>();

                    if (label != null)
                    {
                        label.text = "HIT";
                        label.color = new Color(1f, 0.92f, 0.62f);
                    }

                    SetRect(child as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(128f, 24f));
                }
            }
        }

        private int ExtractTrailingIndex(string value)
        {
            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(value[i]))
                {
                    if (i == value.Length - 1)
                        return 0;

                    return int.Parse(value.Substring(i + 1));
                }
            }

            return 0;
        }

        private void HideLegacyBoardObjects()
        {
            HideCanvasChild("Title");
            HideCanvasChild("Hit Button");
            HideCanvasChild("Action Info Panel");

            if (hitButton != null)
                hitButton.gameObject.SetActive(false);
        }

        private void HideActionInfoPanel()
        {
            if (actionInfoText == null)
                return;

            if (actionInfoText.transform.parent != null && actionInfoText.transform.parent != _canvas.transform)
                actionInfoText.transform.parent.gameObject.SetActive(false);
            else
                actionInfoText.gameObject.SetActive(false);
        }

        private void HideCanvasChild(string objectName)
        {
            if (_canvas == null)
                return;

            Transform child = _canvas.transform.Find(objectName);

            if (child != null)
                child.gameObject.SetActive(false);
        }

        private void CreateRuntimeRoundBanner()
        {
            Image panel = CreateRuntimePanel("Round Banner", _canvas.transform, new Color(0.035f, 0.04f, 0.055f, 0.94f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(560f, 96f));

            roundBannerGroup = panel.gameObject.AddComponent<CanvasGroup>();
            roundBannerGroup.alpha = 0f;
            roundBannerGroup.gameObject.SetActive(false);

            roundBannerText = CreateRuntimeText("Round Banner Text", panel.transform, "ROUND", 24, TextAnchor.MiddleCenter, Color.white);
            roundBannerText.resizeTextForBestFit = true;
            roundBannerText.resizeTextMinSize = 14;
            roundBannerText.resizeTextMaxSize = 24;
            StretchWithInset(roundBannerText.rectTransform, new Vector2(20f, 10f), new Vector2(-20f, -10f));
        }

        private void EnsureEffectTooltip()
        {
            if (_canvas == null || _effectTooltipRoot != null)
                return;

            Image panel = CreateRuntimePanel("Effect Tooltip", _canvas.transform, new Color(0.035f, 0.03f, 0.045f, 0.96f));
            _effectTooltipRoot = panel.rectTransform;
            SetRect(_effectTooltipRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 92f));

            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -5f);

            _effectTooltipGroup = panel.gameObject.AddComponent<CanvasGroup>();
            _effectTooltipGroup.alpha = 0f;
            _effectTooltipGroup.blocksRaycasts = false;
            _effectTooltipGroup.gameObject.SetActive(false);

            _effectTooltipTitleText = CreateRuntimeText("Title", panel.transform, string.Empty, 15, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.56f));
            SetRect(_effectTooltipTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(-28f, 24f));
            StretchHorizontal(_effectTooltipTitleText.rectTransform, 14f, 12f);

            _effectTooltipBodyText = CreateRuntimeText("Body", panel.transform, string.Empty, 11, TextAnchor.UpperLeft, new Color(0.86f, 0.88f, 0.92f));
            _effectTooltipBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _effectTooltipBodyText.verticalOverflow = VerticalWrapMode.Truncate;
            StretchWithInset(_effectTooltipBodyText.rectTransform, new Vector2(14f, 8f), new Vector2(-12f, -34f));
        }

        private Image CreateRuntimePanel(string name, Transform parent, Color color)
        {
            Image image = CreateRuntimeImage(name, parent, panelSprite, color);
            image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private Image CreateRuntimeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            return image;
        }

        private Text CreateRuntimeText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = GetFont();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;

            return text;
        }

        private Font ResolveExistingFont()
        {
            if (messageText != null && messageText.font != null)
                return messageText.font;

            if (playerInfoText != null && playerInfoText.font != null)
                return playerInfoText.font;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private Sprite ResolvePanelSprite()
        {
            if (hitButton == null)
                return null;

            Image buttonImage = hitButton.GetComponent<Image>();
            return buttonImage != null ? buttonImage.sprite : null;
        }

        private void StartExclusive(IEnumerator routine)
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(RunExclusive(routine));
        }

        private IEnumerator RunExclusive(IEnumerator routine)
        {
            yield return routine;
            _activeRoutine = null;
        }

        private Material CreateRevealMaterial()
        {
            if (_cardRevealShader == null)
                _cardRevealShader = Resources.Load<Shader>(CardRevealShaderPath);

            if (_cardRevealShader == null)
                _cardRevealShader = Shader.Find("BlackJackFFLite/UI/CardReveal");

            return _cardRevealShader != null ? new Material(_cardRevealShader) : null;
        }

        private static void SetRevealMaterial(Material material, float reveal, float glow)
        {
            if (material == null)
                return;

            material.SetFloat("_RevealAmount", reveal);
            material.SetFloat("_GlowAmount", glow);
        }

        private Vector2 GetCanvasPosition(RectTransform rect)
        {
            if (_canvasRect == null || rect == null)
                return Vector2.zero;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void UpdateDeckCount()
        {
            if (deckCountText != null && _deck != null)
                deckCountText.text = $"{_deck.CardsRemaining} cards";
        }

        private void SetMessage(string value)
        {
            if (messageText != null)
                messageText.text = value;
        }

        private void HideRoundBanner()
        {
            if (roundBannerGroup == null)
                return;

            roundBannerGroup.alpha = 0f;
            roundBannerGroup.gameObject.SetActive(false);
        }

        private Font GetFont()
        {
            if (uiFont != null)
                return uiFont;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void SetObjectActive(Selectable selectable, bool active)
        {
            if (selectable != null)
                selectable.gameObject.SetActive(active);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StretchWithInset(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void StretchHorizontal(RectTransform rect, float left, float right)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(-(left + right), 24f);
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
                return;

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
    }
}
