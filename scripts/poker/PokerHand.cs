using Godot;

namespace GFrameworkGodotTemplate.scripts.poker;

/// <summary>
///     手牌区控制器
///     自动将子 Poker 卡牌排列成扇形手牌布局
///     当卡牌被放置后自动重新排列剩余卡牌
/// </summary>
[Log]
public partial class PokerHand : Control
{
    [Export] public float CardSpacing = 0.35f;
    [Export] public float MaxFanAngle = 5f;

    private bool _isArranging;

    public override void _Ready()
    {
        ConnectCardSignals();
        CallDeferred(nameof(ArrangeCards));
    }

    /// <summary>
    ///     重新排列所有可见卡牌
    /// </summary>
    public void ArrangeCards()
    {
        if (_isArranging) return;
        _isArranging = true;

        var cards = GetVisibleCards();
        var count = cards.Count;

        if (count == 0)
        {
            _isArranging = false;
            return;
        }

        var cardWidth = cards[0].Size.X;
        var totalWidth = count == 1
            ? cardWidth
            : cardWidth + (count - 1) * cardWidth * CardSpacing;

        var startX = (Size.X - totalWidth) / 2f;
        var centerY = Size.Y / 2f;

        for (var i = 0; i < count; i++)
        {
            var card = cards[i];
            var targetX = startX + i * cardWidth * CardSpacing;
            var targetPos = new Vector2(targetX, centerY - card.Size.Y / 2f);

            var t = count == 1 ? 0.5f : (float)i / (count - 1);
            var fanAngle = Mathf.Lerp(-MaxFanAngle, MaxFanAngle, t);

            card.SetDefaultPosition(targetPos);
            card.SetDefaultRotation(fanAngle);
            card.ResetPosAndRot();
        }

        _isArranging = false;
    }

    private List<Poker> GetVisibleCards()
    {
        var cards = new List<Poker>();
        foreach (var child in GetChildren())
        {
            if (child is Poker poker && poker.Visible)
                cards.Add(poker);
        }
        return cards;
    }

    private void ConnectCardSignals()
    {
        foreach (var child in GetChildren())
        {
            if (child is Poker poker)
                poker.CardPlaced += OnCardPlaced;
        }

        ChildEnteredTree += OnChildAdded;
        ChildExitingTree += OnChildRemoved;
    }

    private void OnCardPlaced()
    {
        CallDeferred(nameof(ArrangeCards));
    }

    private void OnChildAdded(Node node)
    {
        if (node is Poker poker)
        {
            poker.CardPlaced += OnCardPlaced;
            CallDeferred(nameof(ArrangeCards));
        }
    }

    private void OnChildRemoved(Node node)
    {
        if (node is Poker poker)
            poker.CardPlaced -= OnCardPlaced;
    }
}
