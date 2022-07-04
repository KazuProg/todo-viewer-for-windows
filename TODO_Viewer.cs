using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

class ToDoViewer{
	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", EntryPoint = "GetWindowText", CharSet = CharSet.Auto)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	public static bool Running = true;
	private static MainForm form;

	[System.STAThread]
	public static void Main(String[] args){
		int interval = 500;
		int width = 300;
		if(args.Length >= 1){
			try{
				interval = Int32.Parse(args[0]);
			}catch{}
		}
		if(args.Length >= 2){
			try{
				width = Int32.Parse(args[1]);
			}catch{}
		}
		form = new MainForm(width);

		//Windowsメニュー監視スレッド
		Thread thread = new Thread(new ThreadStart(() => {
			WindowsMenuMonitoring(interval);
		}));
		thread.Start();

		Application.Run(ToDoViewer.form);
	}

	private static void WindowsMenuMonitoring(int interval){
		while(ToDoViewer.Running){
			StringBuilder sb = new StringBuilder(65535);//65535に特に意味はない
			GetWindowText(GetForegroundWindow(), sb, 65535);
			if(sb.ToString() == "検索"
			|| sb.ToString() == form.Text){
				form.ShowTODO();
			}else{
				form.HideTODO();
			}
			System.Threading.Thread.Sleep(interval);
		}
	}
}

class MainForm : Form{
	private int width;
	private WebView2 webView = new WebView2();
	private bool isInitialized = false;

	public MainForm(int width)
	{
		this.width = width;
		this.Closed += new EventHandler(this_Closed);
		this.FormBorderStyle = FormBorderStyle.None;
		this.StartPosition = FormStartPosition.Manual;
		this.Text = "TODO Viewer v20220519 - KazuProg";
		this.TopMost = true;
		this.WindowState = FormWindowState.Minimized;//ShowTODOの判定対策

		this.webView.Location = new Point(0, 0);
		this.Controls.Add(this.webView);

		InitializeAsync();
		//this.webView.CoreWebView.OpenDevToolsWindow();
	}

	public void ShowTODO(){
		if(this.WindowState != FormWindowState.Normal
		&& this.isInitialized){
			var workingArea = Screen.PrimaryScreen.WorkingArea;
			var size = new Size(this.width, workingArea.Height);
			this.Size = size;
			this.Top = workingArea.Top;

			//タスクバーが右端なら
			if(Screen.PrimaryScreen.Bounds.Height == workingArea.Height
			&& workingArea.Left == 0){
				//左側に表示
				this.Left = 0;
			}else{
				this.Left = workingArea.Left + workingArea.Width - this.width;
			}

			this.webView.Size = size;

			//https://docs.microsoft.com/ja-jp/dotnet/api/system.invalidoperationexception?view=net-6.0
			bool uiMarshal = this.webView.InvokeRequired;
			if (!uiMarshal){
				this.webView.CoreWebView2.Navigate("http://kazuprog.work/prog/web/todo/?platform=winexe");
			}else{
				this.webView.Invoke(new Action(() => {
					this.webView.CoreWebView2.Navigate("http://kazuprog.work/prog/web/todo/?platform=winexe");
				}));
			}

			this.WindowState = FormWindowState.Normal;
		}
	}

	public void HideTODO(){
		if(this.WindowState != FormWindowState.Minimized){
			this.WindowState = FormWindowState.Minimized;
		}
	}

	private void this_Closed(object ender, EventArgs e){
		ToDoViewer.Running = false;
	}

	async void InitializeAsync()
	{
		await webView.EnsureCoreWebView2Async(null);
		this.isInitialized = true;
		ShowTODO();
	}
}	