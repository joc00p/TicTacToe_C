using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace TicTacToe_C;

public partial class MainWindow : Window
{
    // ── Board colours ─────────────────────────────────────────────────────
    static readonly Color XColor  = (Color)ColorConverter.ConvertFromString("#60CDFF");
    static readonly Color OColor  = (Color)ColorConverter.ConvertFromString("#FF6B81");
    static readonly Color AccCol  = (Color)ColorConverter.ConvertFromString("#BC8CFF");

    // ── Win-line endpoints in the 330×330 board coordinate space ─────────
    // UniformGrid: 3 cols × 110px each. Cell centres: 55, 165, 275.
    static readonly (double x1, double y1, double x2, double y2)[] WinCoords =
    [
        (35,  55, 295,  55),   // row 0
        (35, 165, 295, 165),   // row 1
        (35, 275, 295, 275),   // row 2
        (55,  35,  55, 295),   // col 0
        (165,  35, 165, 295),  // col 1
        (275,  35, 275, 295),  // col 2
        (35,  35, 295, 295),   // diagonal ↘
        (295,  35,  35, 295),  // diagonal ↙
    ];

    // ── Game state ────────────────────────────────────────────────────────
    private int[]  _board    = new int[9];
    private bool   _gameOver = true;
    private bool   _busy     = false;
    private int    _turn;
    private int    _level    = 3;
    private int[]  _scores   = [0, 0, 0];   // player, draw, cpu

    // ── UI references ─────────────────────────────────────────────────────
    private readonly Button[] _cells    = new Button[9];
    private readonly Canvas[] _canvases = new Canvas[9];

    static readonly string[] LevelLabels =
        ["Lvl 1\nRandom", "Lvl 2\n1-ply", "Lvl 3\nMedium", "Lvl 4\n3-ply", "Lvl 5\nUnbeatable"];

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => MicaHelper.TryApply(this);
        BuildBoard();
        BuildLevelPills();
    }

    // ── Build 9 board cells ───────────────────────────────────────────────
    private void BuildBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            var canvas = new Canvas { Width = 76, Height = 76 };
            _canvases[i] = canvas;

            var btn = new Button
            {
                Style   = (Style)Resources["CellStyle"],
                Content = canvas,
                Margin  = new Thickness(5),
                Tag     = i
            };
            btn.Click += Cell_Click;
            _cells[i] = btn;
            BoardGrid.Children.Add(btn);
        }
    }

    // ── Build level radio pills ───────────────────────────────────────────
    private void BuildLevelPills()
    {
        for (int i = 0; i < 5; i++)
        {
            int lv = i + 1;
            var rb = new RadioButton
            {
                Content   = LevelLabels[i],
                Style     = (Style)Resources["PillStyle"],
                GroupName = "Level",
                IsChecked = lv == _level,
                Margin    = new Thickness(i == 0 ? 0 : 3, 0, i == 4 ? 0 : 3, 0),
                Tag       = lv
            };
            rb.Checked += (s, _) => { if (s is RadioButton r && r.Tag is int l) _level = l; };
            LevelGrid.Children.Add(rb);
        }
    }

    // ── New Game ──────────────────────────────────────────────────────────
    private async void NewGame_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        _board    = new int[9];
        _gameOver = false;
        _busy     = false;
        WinCanvas.Children.Clear();

        for (int i = 0; i < 9; i++)
        {
            _canvases[i].Children.Clear();
            _cells[i].IsEnabled = true;
            SetWinGlow(_cells[i], false, GameEngine.Empty);
        }

        _turn = RbFirstPlayer.IsChecked == true ? GameEngine.Player : GameEngine.Computer;

        if (_turn == GameEngine.Player)
        {
            SetStatus("Your turn", XColor);
        }
        else
        {
            SetStatus("Thinking...", OColor);
            LockBoard(true);
            await Task.Delay(460);
            if (!_gameOver) await DoCpuMove();
        }
    }

    // ── Cell clicked ──────────────────────────────────────────────────────
    private async void Cell_Click(object sender, RoutedEventArgs e)
    {
        if (_gameOver || _busy || _turn != GameEngine.Player) return;
        if (sender is not Button btn || btn.Tag is not int idx) return;
        if (_board[idx] != GameEngine.Empty) return;

        _busy = true;
        PlaceMark(idx, GameEngine.Player);

        if (!_gameOver)
        {
            _turn = GameEngine.Computer;
            SetStatus("Thinking...", OColor);
            LockBoard(true);
            await Task.Delay(_level == 5 ? 520 : 340);
            if (!_gameOver) await DoCpuMove();
        }
        _busy = false;
    }

    private Task DoCpuMove()
    {
        int move = GameEngine.GetComputerMove(_board, _level);
        PlaceMark(move, GameEngine.Computer);
        return Task.CompletedTask;
    }

    // ── Place mark and evaluate ───────────────────────────────────────────
    private void PlaceMark(int idx, int who)
    {
        _board[idx] = who;
        _cells[idx].IsEnabled = false;

        if (who == GameEngine.Player) DrawX(_canvases[idx]);
        else                          DrawO(_canvases[idx]);

        var winLine = GameEngine.GetWinLine(_board);
        if (winLine != null)
        {
            _gameOver = true;
            foreach (int wi in winLine)
                SetWinGlow(_cells[wi], true, who);

            // Find which WIN_LINE index this is
            int lineIdx = Array.IndexOf(GameEngine.WinLines, winLine);
            AnimateWinLine(lineIdx, who);

            if (who == GameEngine.Player)
            {
                _scores[0]++;
                UpdateScore(ScorePlayer, _scores[0]);
                SetStatus("You win!", XColor, 17);
            }
            else
            {
                _scores[2]++;
                UpdateScore(ScoreCpu, _scores[2]);
                SetStatus("Computer wins!", OColor, 17);
            }
            LockBoard(false);
            return;
        }

        if (GameEngine.GetEmpty(_board).Count == 0)
        {
            _gameOver = true;
            _scores[1]++;
            UpdateScore(ScoreDraw, _scores[1]);
            SetStatus("It's a draw!", AccCol, 15);
            LockBoard(false);
            return;
        }

        _turn = who == GameEngine.Player ? GameEngine.Computer : GameEngine.Player;
        if (_turn == GameEngine.Player)
        {
            LockBoard(false);
            SetStatus("Your turn", XColor);
        }
    }

    // ── Draw X with flip-in animation ─────────────────────────────────────
    // Simulates a 3D flip by animating ScaleX 0 → 1 with BackEase overshoot,
    // combined with the mark appearing as if turning to face the viewer.
    private void DrawX(Canvas c)
    {
        var brush = new SolidColorBrush(XColor);
        var glow  = MakeGlow(XColor);

        var l1 = new Line { X1=12, Y1=12, X2=64, Y2=64, Stroke=brush, StrokeThickness=7,
            StrokeStartLineCap=PenLineCap.Round, StrokeEndLineCap=PenLineCap.Round, Effect=glow };
        var l2 = new Line { X1=64, Y1=12, X2=12, Y2=64, Stroke=brush, StrokeThickness=7,
            StrokeStartLineCap=PenLineCap.Round, StrokeEndLineCap=PenLineCap.Round, Effect=glow };

        c.Children.Add(l1);
        c.Children.Add(l2);
        FlipIn(l1, 0);
        FlipIn(l2, 120);
    }

    // ── Draw O with flip-in animation ─────────────────────────────────────
    private void DrawO(Canvas c)
    {
        var brush = new SolidColorBrush(OColor);
        var glow  = MakeGlow(OColor);

        var e = new Ellipse { Width=52, Height=52, Stroke=brush, StrokeThickness=7,
            Fill=Brushes.Transparent, Effect=glow };
        Canvas.SetLeft(e, 12); Canvas.SetTop(e, 12);
        c.Children.Add(e);
        FlipIn(e, 0);
    }

    // ── Flip-in: ScaleX sweeps 0→1.08→1 (simulates piece turning to face you) ──
    private static void FlipIn(UIElement el, int delayMs)
    {
        el.RenderTransformOrigin = new Point(0.5, 0.5);
        var st = new ScaleTransform(0, 1);
        el.RenderTransform = st;

        var anim = new DoubleAnimationUsingKeyFrames
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.1,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210)),
            new CubicEase { EasingMode = EasingMode.EaseOut }));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
            new QuadraticEase { EasingMode = EasingMode.EaseOut }));

        st.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
    }

    // ── Win line: animated stroke drawn across the winning cells ──────────
    private void AnimateWinLine(int lineIdx, int who)
    {
        if (lineIdx < 0 || lineIdx >= WinCoords.Length) return;
        var (x1, y1, x2, y2) = WinCoords[lineIdx];
        var col = who == GameEngine.Player ? XColor : OColor;

        double len = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)) + 10;

        var line = new Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            Stroke = new SolidColorBrush(col),
            StrokeThickness = 5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap   = PenLineCap.Round,
            StrokeDashArray    = new DoubleCollection { len / 5, len / 5 },
            StrokeDashOffset   = len / 5,
            Opacity            = 0.82,
            Effect = new DropShadowEffect { Color = col, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.8 }
        };
        WinCanvas.Children.Add(line);

        var anim = new DoubleAnimation(len / 5, 0, new Duration(TimeSpan.FromMilliseconds(480)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        line.BeginAnimation(Shape.StrokeDashOffsetProperty, anim);
    }

    // ── Win-cell glow ─────────────────────────────────────────────────────
    private static void SetWinGlow(Button btn, bool on, int who)
    {
        if (!on) { btn.Effect = null; return; }
        var col = who == GameEngine.Player ? XColor : OColor;
        btn.Effect = new DropShadowEffect { Color = col, BlurRadius = 22, ShadowDepth = 0, Opacity = 0.7 };
    }

    // ── Score pop animation ───────────────────────────────────────────────
    private static void UpdateScore(System.Windows.Controls.TextBlock tb, int value)
    {
        tb.Text = value.ToString();
        var st  = (ScaleTransform)tb.RenderTransform;
        var kf  = new DoubleAnimationUsingKeyFrames();
        var ease = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(1.55, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), ease));
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)),
            new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        st.BeginAnimation(ScaleTransform.ScaleXProperty, kf);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, kf);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void SetStatus(string text, Color color, double fontSize = 13.5)
    {
        StatusText.Text      = text;
        StatusText.Foreground = new SolidColorBrush(color);
        StatusText.FontSize   = fontSize;
    }

    private void LockBoard(bool locked)
    {
        for (int i = 0; i < 9; i++)
            if (_board[i] == GameEngine.Empty)
                _cells[i].IsEnabled = !locked;
    }

    private static DropShadowEffect MakeGlow(Color col) =>
        new() { Color = col, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.75 };
}
