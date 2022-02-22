namespace OsuDiffCalc.UserInterface {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

#pragma warning disable IDE1006 // Naming Styles
	[DesignerCategory("")]
	public class ValidatingTextBox : TextBox {
		private string _validText;
		private int _selectionStart;
		private int _selectionEnd;
		private bool _dontProcessMessages;

		public event EventHandler<TextValidatingEventArgs> TextValidating;

		protected virtual void OnTextValidating(TextValidatingEventArgs e) {
			TextValidating?.Invoke(this, e);
		}

		protected override void WndProc(ref Message m) {
			base.WndProc(ref m);
			if (_dontProcessMessages)
				return;

			const int WM_KEYDOWN = 0x100;
			const int WM_ENTERIDLE = 0x121;
			const int VK_DELETE = 0x2e;

			bool delete = m.Msg == WM_KEYDOWN && (int)m.WParam == VK_DELETE;
			if ((m.Msg == WM_KEYDOWN && !delete) || m.Msg == WM_ENTERIDLE) {
				DontProcessMessage(() => {
					_validText = Text;
					_selectionStart = SelectionStart;
					_selectionEnd = SelectionLength;
				});
			}

			const int WM_CHAR = 0x102;
			const int WM_PASTE = 0x302;
			if (m.Msg == WM_CHAR || m.Msg == WM_PASTE || delete) {
				string newText = null;
				DontProcessMessage(() => newText = Text);

				var e = new TextValidatingEventArgs(newText);
				OnTextValidating(e);
				if (!e.IsValid) {
					DontProcessMessage(() => {
						Text = _validText;
						SelectionStart = _selectionStart;
						SelectionLength = _selectionEnd;
					});
				}
			}
		}

		protected override void OnLostFocus(EventArgs e) {
			var e2 = new TextValidatingEventArgs(Text);
			OnTextValidating(e2);
			base.OnLostFocus(e);
		}

		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
			if (Enabled && e.KeyCode is Keys.Enter or Keys.Escape) {
				var form = FindForm();
				if (form is not null) {
					form.ActiveControl = null;
				}
				else {
					Enabled = false;
					Enabled = true;
				}
				Focus();
			}
			else
				base.OnPreviewKeyDown(e);
		}

		private void DontProcessMessage(Action action) {
			try {
				_dontProcessMessages = true;
				action();
			}
			finally {
				_dontProcessMessages = false;
			}
		}
	}

	public class TextValidatingEventArgs : EventArgs {
		public TextValidatingEventArgs(string newText, bool isValid = true) {
			NewText = newText;
			IsValid = isValid;
		}

		public bool IsValid { get; set; } = true;
		public string NewText { get; }
	}
}
