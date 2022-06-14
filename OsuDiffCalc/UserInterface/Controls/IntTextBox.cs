namespace OsuDiffCalc.UserInterface.Controls {
	using System.Globalization;

	/// <summary>
	/// Validating TextBox which only accepts valid <see cref="int"/> input
	/// </summary>
	public class IntTextBox : NumericTextBox<int> {
		private bool _textValidating = false;

		protected override void OnTextValidating(TextValidatingEventArgs e) {
			if (_textValidating) return;
			base.OnTextValidating(e);
			try {
				_textValidating = true;
				e.IsValid = int.TryParse(e.NewText, _numberStyle, CultureInfo.CurrentUICulture, out var value);
				if (e.IsValid)
					Value = value;
			}
			finally {
				_textValidating = false;
			}
		}
	}
}
