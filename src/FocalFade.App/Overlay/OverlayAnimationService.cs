using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FocalFade.Overlay;

public static class OverlayAnimationService
{
    public static void AnimateOpacity(UIElement element, double from, double to, int durationMs, Action? onComplete = null)
    {
        if (durationMs <= 0)
        {
            element.Opacity = to;
            onComplete?.Invoke();
            return;
        }

        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        if (onComplete != null)
            animation.Completed += (_, _) => onComplete();

        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    public static void AnimatePosition(FrameworkElement element, double fromX, double fromY, double toX, double toY, int durationMs)
    {
        if (durationMs <= 0)
        {
            Canvas.SetLeft(element, toX);
            Canvas.SetTop(element, toY);
            return;
        }

        var animX = new DoubleAnimation
        {
            From = fromX,
            To = toX,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        var animY = new DoubleAnimation
        {
            From = fromY,
            To = toY,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        element.BeginAnimation(Canvas.LeftProperty, animX);
        element.BeginAnimation(Canvas.TopProperty, animY);
    }

    public static Storyboard CreateFadeInStoryboard(double targetOpacity, int durationMs)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 0,
            To = targetOpacity,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(animation);
        return storyboard;
    }

    public static Storyboard CreateFadeOutStoryboard(int durationMs)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(animation);
        return storyboard;
    }
}
