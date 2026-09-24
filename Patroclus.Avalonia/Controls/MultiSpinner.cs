using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Globalization;

namespace Patroclus.Avalonia.Controls
{
    //DH1KLM: MultiSpinner uses the public Avalonia TextBox API so it is independent
    //DH1KLM: of Avalonia's internal text presenter implementation.
    public class MultiSpinner : TextBox
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<MultiSpinner, double>(
                nameof(Value),
                defaultValue: 0,
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<MultiSpinner, double>(
                nameof(Maximum),
                defaultValue: 100);

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<MultiSpinner, double>(
                nameof(Minimum),
                defaultValue: 0);

        public static readonly StyledProperty<bool> SpinnerIsReadOnlyProperty =
            AvaloniaProperty.Register<MultiSpinner, bool>(
                nameof(SpinnerIsReadOnly),
                defaultValue: false);

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        //DH1KLM: Keep the original public IsReadOnly behavior without hiding TextBox.IsReadOnly.
        public bool SpinnerIsReadOnly
        {
            get => GetValue(SpinnerIsReadOnlyProperty);
            set => SetValue(SpinnerIsReadOnlyProperty, value);
        }

        private bool _updatingText;
        private Point _lastPoint;

        static MultiSpinner()
        {
            ValueProperty.Changed.AddClassHandler<MultiSpinner>((control, change) =>
            {
                control.UpdateTextFromValue(change.NewValue.GetValueOrDefault<double>());
            });

            MaximumProperty.Changed.AddClassHandler<MultiSpinner>((control, _) =>
                control.NormalizeValueAndText());

            MinimumProperty.Changed.AddClassHandler<MultiSpinner>((control, _) =>
                control.NormalizeValueAndText());
        }

        public MultiSpinner()
        {
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            TextAlignment = Avalonia.Media.TextAlignment.Right;
            IsReadOnly = true;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            UpdateTextFromValue(Value);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (SpinnerIsReadOnly || IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            HandleDigitInput(e.Text);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var modifiers = e.KeyModifiers;

            switch (e.Key)
            {
                case Key.Up:
                    IncrementAtCaret(1);
                    e.Handled = true;
                    return;
                case Key.Down:
                    IncrementAtCaret(-1);
                    e.Handled = true;
                    return;
                case Key.Delete:
                    SetDigitAtCaret(0);
                    e.Handled = true;
                    return;
                case Key.Back:
                    if (CaretIndex > 0)
                    {
                        CaretIndex--;
                        SetDigitAtCaret(0);
                    }
                    e.Handled = true;
                    return;
                case Key.Home:
                    CaretIndex = 0;
                    e.Handled = true;
                    return;
                case Key.End:
                    CaretIndex = Text?.Length ?? 0;
                    e.Handled = true;
                    return;
                case Key.C:
                    if (modifiers == KeyModifiers.Control)
                    {
                        Copy();
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.V:
                    if (modifiers == KeyModifiers.Control)
                    {
                        Paste();
                        e.Handled = true;
                        return;
                    }
                    break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _lastPoint = e.GetPosition(this);
            base.OnPointerPressed(e);

            if (SpinnerIsReadOnly || !IsEffectivelyEnabled)
                return;

            if (_lastPoint.Y < Bounds.Height * 0.25)
                IncrementAtCaret(1);
            else if (_lastPoint.Y > Bounds.Height * 0.75)
                IncrementAtCaret(-1);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (!SpinnerIsReadOnly && IsEffectivelyEnabled)
            {
                IncrementAtCaret(Math.Sign(e.Delta.Y));
                e.Handled = true;
                return;
            }

            base.OnPointerWheelChanged(e);
        }

        private void HandleDigitInput(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            foreach (var character in input)
            {
                if (character < '0' || character > '9')
                    continue;

                SetDigitAtCaret(character - '0');
                if (CaretIndex < (Text?.Length ?? 0))
                    CaretIndex++;
            }
        }

        private void IncrementAtCaret(int direction)
        {
            if (SpinnerIsReadOnly && !IsFocused)
                return;

            var textLength = Text?.Length ?? 0;
            if (textLength == 0)
                return;

            var index = Math.Clamp(CaretIndex, 0, textLength - 1);
            var power = textLength - index - 1;
            var step = Math.Pow(10, power);
            SetValueClamped(Value + direction * step);
        }

        private void SetDigitAtCaret(int digit)
        {
            var text = GetDisplayText();
            if (text.Length == 0)
                return;

            var index = Math.Clamp(CaretIndex, 0, text.Length - 1);
            var power = text.Length - index - 1;
            var multiplier = Math.Pow(10, power);
            var currentDigit = (int)(Value / multiplier) % 10;
            SetValueClamped(Value + (digit - currentDigit) * multiplier);
        }

        private void SetValueClamped(double value)
        {
            Value = Math.Clamp(value, Minimum, Maximum);
        }

        private string GetDisplayText()
        {
            var places = GetPlaces();
            var integer = Math.Max(0, (long)Math.Round(Value));
            return integer.ToString(new string('0', places), CultureInfo.InvariantCulture);
        }

        private int GetPlaces()
        {
            var maximum = Math.Max(1, Math.Abs(Maximum));
            return Math.Max(1, (int)Math.Floor(Math.Log10(maximum)) + 1);
        }

        private void UpdateTextFromValue(double value)
        {
            if (_updatingText)
                return;

            try
            {
                _updatingText = true;
                Text = Math.Clamp(value, Minimum, Maximum)
                    .ToString(new string('0', GetPlaces()), CultureInfo.InvariantCulture);
            }
            finally
            {
                _updatingText = false;
            }
        }

        private void NormalizeValueAndText()
        {
            if (Maximum < Minimum)
                return;

            SetValueClamped(Value);
            UpdateTextFromValue(Value);
        }

        private async void Copy()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(GetDisplayText());
        }

        private async void Paste()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
                return;

            var text = await clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var digits = new string(text.Where(char.IsDigit).ToArray());
                if (double.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    SetValueClamped(value);
            }
        }
    }
}