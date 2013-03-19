using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Amazon.Inapp.Purchasing;
using System.Threading.Tasks;

namespace com.amazon.sample.buttonclicker
{
	public class DialogCommandWrapper : Java.Lang.Object, IDialogInterfaceOnClickListener
	{
		Task m_task = null;

		public Dialog CreateConfirmationDialog(Context ctx, string title, string confirmText, string dismissText, Task cmd)
		{
			m_task = cmd;

			var builder = new AlertDialog.Builder(ctx);
			builder.SetCancelable(true);
			builder.SetIcon(Resource.Drawable.icon);
			builder.SetTitle(title);
			builder.SetInverseBackgroundForced(true);
			builder.SetPositiveButton(confirmText, this);
			builder.SetNegativeButton(dismissText, this);
			
			return builder.Create();
		}

		public void OnClick(IDialogInterface dialog, int which)
		{
			dialog.Dismiss();

			if (which == (int)Android.Content.DialogButtonType.Positive)
				m_task.Start();
		}

		public void Dispose()
		{
			base.Dispose();
		}

		public IntPtr Handle
		{
			get { return base.Handle; }
		}
	}
}