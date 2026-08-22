using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace LADXHD_Launcher
{
    public static class UiNavigator
    {
        public enum NavAction { Up, Down, Left, Right, Accept, Cancel, AdjustUp, AdjustDown, FocusBack, CategoryNext, CategoryPrev, ShowTip }
        private static List<InputElement>? _candidates;
        private static Control? _openTip;
        private static Window? _candidatesOwner;

        // Call when the page content swaps or generated controls are rebuilt.
        public static void InvalidateCandidates()
        {
            _candidates = null;
            _candidatesOwner = null;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        NAVIGATION FUNCTIONS - CONROLLER BUTTONS ARE PRESSED
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static ComboBox? OpenCombo(IInputElement? focused, Window window)
        {
            if (focused is ComboBox c && c.IsDropDownOpen) return c;
            if (focused is Visual v && v.FindAncestorOfType<ComboBox>() is ComboBox c2 && c2.IsDropDownOpen) return c2;
            return window.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault(cb => cb.IsDropDownOpen);
        }

        private static bool ComboBoxAction(NavAction action, ComboBox combo)
        {
            switch (action)
            {
                case NavAction.Up:
                    if (combo.SelectedIndex > 0) combo.SelectedIndex--;
                    return true;
                case NavAction.Down:
                    if (combo.SelectedIndex < combo.ItemCount - 1) combo.SelectedIndex++;
                    return true;
                case NavAction.Accept:
                case NavAction.Cancel:
                    combo.IsDropDownOpen = false;
                    combo.Focus(NavigationMethod.Directional);
                    return true;
                default:
                    // Y / LT / RT / X / Left / Right: close it, then run the action normally.
                    combo.IsDropDownOpen = false;
                    break;
            }
            return false;
        }

        public static bool Dispatch(NavAction action, Window window)
        {
            var fm = window.FocusManager;            // IFocusManager — GetFocusedElement() is public here
            if (fm is null) return false;

            var focused = fm.GetFocusedElement();

            // Any action other than X dismisses a forced tooltip.
            if (action != NavAction.ShowTip)
                CloseTip();

            if (focused is TextBox tb && tb.FindAncestorOfType<NumericUpDown>() is null)
                return false;

            if (OpenCombo(focused, window) is ComboBox openCombo)
            {
                return ComboBoxAction(action, openCombo);
            }

            switch (action)
            {
                case NavAction.Up:           return Move(window, focused, NavigationDirection.Up);
                case NavAction.Down:         return Move(window, focused, NavigationDirection.Down);
                case NavAction.Left:         return Move(window, focused, NavigationDirection.Left);
                case NavAction.Right:        return Move(window, focused, NavigationDirection.Right);
                case NavAction.Accept:       return Activate(focused);
                case NavAction.AdjustUp:     return Adjust(focused, +1);
                case NavAction.AdjustDown:   return Adjust(focused, -1);
                case NavAction.Cancel:       return Cancel(window);
                case NavAction.FocusBack:    return FocusBack(window);
                case NavAction.CategoryNext: return SkipCategory(window, focused, +1);
                case NavAction.CategoryPrev: return SkipCategory(window, focused, -1);
                case NavAction.ShowTip:      return ShowTip(focused, window);
            }
            return false;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        NAVIGATION - MOVE THE SELECTION WITH DPAD OR NUMPAD ARROW KEYS (2,4,6,8)
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static Rect? GetRect(Visual v, Visual relativeTo)
        {
            if (v.TranslatePoint(new Point(0, 0), relativeTo) is not Point p) return null;
            return new Rect(p, v.Bounds.Size);
        }

        private static double SpanGap(double a1, double a2, double b1, double b2)
        {
            return Math.Max(0, Math.Max(a1, b1) - Math.Min(a2, b2));
        }

        private static InputElement? SearchBest(Window window, List<InputElement> candidates, IInputElement? focused, 
            NavigationDirection dir, double ccx, double ccy, Rect cur, Func<InputElement, bool> inScope)
        {
            InputElement? bestAligned = null;
            InputElement? bestOff = null; 

            double bestAlignedDist = double.MaxValue;
            double bestOffScore = double.MaxValue;

            foreach (var cand in candidates)
            {
                if (ReferenceEquals(cand, focused)) continue;
                if (!cand.IsEffectivelyVisible || !cand.IsEffectivelyEnabled) continue;
                if (!inScope(cand)) continue;
                if (GetRect(cand, window) is not Rect r) continue;

                double dx = (r.X + r.Width / 2) - ccx;
                double dy = (r.Y + r.Height / 2) - ccy;

                double primary, cross;
                switch (dir)
                {
                    case NavigationDirection.Right: if (dx <=  1) continue; primary =  dx; cross = SpanGap(cur.Top, cur.Bottom, r.Top, r.Bottom); break;
                    case NavigationDirection.Left:  if (dx >= -1) continue; primary = -dx; cross = SpanGap(cur.Top, cur.Bottom, r.Top, r.Bottom); break;
                    case NavigationDirection.Down:  if (dy <=  1) continue; primary =  dy; cross = SpanGap(cur.Left, cur.Right, r.Left, r.Right); break;
                    case NavigationDirection.Up:    if (dy >= -1) continue; primary = -dy; cross = SpanGap(cur.Left, cur.Right, r.Left, r.Right); break;
                    default: continue;
                }

                if (cross < 0.5)
                {
                    if (primary < bestAlignedDist) { bestAlignedDist = primary; bestAligned = cand; }
                }
                else
                {
                    double score = primary + 2.0 * cross;
                    if (score < bestOffScore) { bestOffScore = score; bestOff = cand; }
                }
            }
            return bestAligned ?? bestOff;
        }

        private static List<InputElement> CollectFocusable(Window window)
        {
            // Reuse unless the page/window changed (auto-rebuilds when a dialog becomes active later).
            if (_candidates is not null && ReferenceEquals(_candidatesOwner, window))
                return _candidates;

            var focusable = window.GetVisualDescendants().OfType<InputElement>().Where(e => e.Focusable).ToList();
            var set = new HashSet<InputElement>(focusable);
            var result = new List<InputElement>(focusable.Count);

            foreach (var e in focusable)
            {
                bool nested = false;
                foreach (var anc in e.GetVisualAncestors())
                {
                    if (anc is InputElement ie && set.Contains(ie)) { nested = true; break; }
                }
                if (!nested) result.Add(e);
            }
            _candidates = result;
            _candidatesOwner = window;
            return result;
        }

        private static bool Move(Window window, IInputElement? focused, NavigationDirection dir)
        {
            var candidates = CollectFocusable(window);
            if (candidates.Count == 0) return false;

            if (focused is not Visual current)
            {
                candidates[0].Focus(NavigationMethod.Directional);
                return true;
            }
            var currentNud = current.FindAncestorOfType<NumericUpDown>();
            Visual rectSource = currentNud ?? current;
            if (GetRect(rectSource, window) is not Rect cur) return false;

            double ccx = cur.X + cur.Width / 2;
            double ccy = cur.Y + cur.Height / 2;

            var scope = current.FindAncestorOfType<ScrollViewer>();
            var best = SearchBest(window, candidates, focused, dir, ccx, ccy, cur, c => c.FindAncestorOfType<ScrollViewer>() == scope)
                    ?? SearchBest(window, candidates, focused, dir, ccx, ccy, cur, c => c.FindAncestorOfType<ScrollViewer>() != scope);

            if (best is null) 
                return false;

            best.Focus(NavigationMethod.Directional);
            return true;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        NAVIGATION - SCROLL WITH THE RIGHT STICK
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        public static void Scroll(Window window, double delta)
        {
            // Pan the page's scrollable region — the first ScrollViewer with content taller than its viewport.
            var sv = window.GetVisualDescendants()
                           .OfType<ScrollViewer>()
                           .FirstOrDefault(s => s.Extent.Height > s.Viewport.Height);
            if (sv is null) return;

            double max = sv.Extent.Height - sv.Viewport.Height;
            double y   = Math.Clamp(sv.Offset.Y + delta, 0, max);
            sv.Offset  = new Vector(sv.Offset.X, y);
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        CONTROL ACTIVATION - PERFORMS THE CONTROL'S ACTION (CHECK CHECKBOX, OPEN COMBOBOX, ETC).
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool Activate(IInputElement? focused)
        {
            switch (focused)
            {
                // MUST precede Button (CheckBox : ToggleButton : Button)
                case CheckBox cb:
                    cb.IsChecked = !(cb.IsChecked == true);
                    return true;
                case Button b:
                    b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    return true;
                case ComboBox combo:
                    combo.IsDropDownOpen = true;
                    return true;
                default:
                    return false;
            }
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        NUMERIC UP/DOWN ADJUSTMENT - CONTROLLER LB/RB OR MOUSE SCROLLWHEEL WHEN HOVERING
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool Adjust(IInputElement? focused, int sign)
        {
            var nud = focused as NumericUpDown
                      ?? (focused as Visual)?.FindAncestorOfType<NumericUpDown>();
            if (nud is null) return false;

            decimal step = nud.Increment == 0 ? 1 : nud.Increment;
            decimal next = (nud.Value ?? 0m) + sign * step;
            nud.Value = Math.Clamp(next, nud.Minimum, nud.Maximum);
            return true;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        CANCEL - NAVIGATE BACKWARDS - RETURN TO MAIN MENU
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool Cancel(Window window)
        {
            if (window is MainWindow main && main.CurrentPage is IControllerPage page)
            {
                page.OnCancel();
                return true;
            }
            window.Close();
            return true;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        JUMP TO BOTTOM BUTTONS
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool FocusBack(Window window)
        {
            if (window is MainWindow main && main.CurrentPage is IControllerPage page)
            {
                page.FocusBack();
                return true;
            }
            return false;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        CATEGORY JUMPING - SKIP CATEGORIES WITH LT/RT
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool HasFocusable(Visual root)
        {
            return root.GetVisualDescendants().OfType<InputElement>().Any(e => e.Focusable && e.IsEffectivelyVisible && e.IsEffectivelyEnabled);
        }

        private static InputElement? FirstFocusable(Control category, Window window)
        {
            // Reuse the outermost-focusable list (handles NumericUpDown's inner editor for us),
            // filtered to this category, ordered top row first then left-most.
            InputElement? best = null;
            double bestKey = double.MaxValue;
            foreach (var e in CollectFocusable(window))
            {
                if (!e.IsEffectivelyVisible || !e.IsEffectivelyEnabled) continue;
                if (!e.GetVisualAncestors().Contains(category)) continue;
                if (GetRect(e, window) is not Rect r) continue;

                double key = r.Y * 100000 + r.X;
                if (key < bestKey) { bestKey = key; best = e; }
            }
            return best;
        }

        private static bool SkipCategory(Window window, IInputElement? focused, int dir)
        {
            // The scroll region holds the vertical stack of category panels.
            var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (scroller?.Content is not Panel content) return false;

            // A category = each direct child of that stack that owns a focusable control.
            var categories = content.Children.OfType<Control>().Where(HasFocusable).ToList();

            // The bottom button bar (Back/Reset/Save/Exit) lives outside the ScrollViewer —
            // tack it on as the final category so the last RT lands there.
            var bottomBar = window.GetVisualDescendants().OfType<Canvas>().FirstOrDefault(c => c.FindAncestorOfType<ScrollViewer>() is null && HasFocusable(c));
            if (bottomBar is not null)
                categories.Add(bottomBar);

            if (categories.Count == 0) return false;

            // Which category currently holds focus? (-1 if focus is somewhere unexpected)
            int curIndex = -1;
            if (focused is Visual fv)
                curIndex = categories.FindIndex(c => ReferenceEquals(fv, c) || fv.GetVisualAncestors().Contains(c));

            // Clamp to the ends — no wrap-around past the first category / the button bar.
            int dest = curIndex < 0
                ? (dir > 0 ? 0 : categories.Count - 1)
                : Math.Clamp(curIndex + dir, 0, categories.Count - 1);
            if (dest == curIndex) return false;

            // Land on the top-left-most control of the destination category.
            var target = FirstFocusable(categories[dest], window);
            if (target is null) return false;

            target.Focus(NavigationMethod.Directional);
            (target as Control)?.BringIntoView();
            return true;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        SHOW / HIDE CONTROL TOOLTIP
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static Control? FindTipOwner(Visual start)
        {
            for (Visual? v = start; v is not null; v = v.GetVisualParent())
                if (v is Control c && ToolTip.GetTip(c) is not null)
                    return c;
            return null;
        }

        private static Control? FindRowTip(Visual focused, Window window)
        {
            var canvas = focused.FindAncestorOfType<Canvas>();
            if (canvas is null || GetRect(focused, window) is not Rect fr) return null;

            double fcy = fr.Y + fr.Height / 2;
            double fcx = fr.X + fr.Width / 2;

            Control? best = null;
            double bestScore = double.MaxValue;
            foreach (var c in canvas.GetVisualDescendants().OfType<Control>())
            {
                if (ToolTip.GetTip(c) is null) continue;
                if (GetRect(c, window) is not Rect r) continue;

                double dy = Math.Abs((r.Y + r.Height / 2) - fcy);
                if (dy > 18) continue;
                double dx = Math.Abs((r.X + r.Width / 2) - fcx);
                double score = dy * 100 + dx;
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        private static void CloseTip()
        {
            if (_openTip is null) return;
            ToolTip.SetIsOpen(_openTip, false);
            _openTip = null;
        }

        private static bool ShowTip(IInputElement? focused, Window window)
        {
            if (focused is not Visual fv) return false;

            // Tip on the focused control / an ancestor (mods controls, checkboxes),
            // else the nearest tip-bearing element on the same row (settings labels).
            var owner = FindTipOwner(fv) ?? FindRowTip(fv, window);

            // Pressing X again on the same control toggles it back off.
            if (_openTip is not null)
            {
                bool same = ReferenceEquals(_openTip, owner);
                CloseTip();
                if (same) return true;
            }
            if (owner is null) return false;

            // Anchor to the control, not the (absent) mouse pointer.
            ToolTip.SetPlacement(owner, PlacementMode.Bottom);
            ToolTip.SetIsOpen(owner, true);
            _openTip = owner;
            return true;
        }
    }
}