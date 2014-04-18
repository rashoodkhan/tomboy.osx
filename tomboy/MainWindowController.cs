using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

using System.IO;
using MonoMac.WebKit;

namespace Tomboy
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		#region fields
		/// <summary>
		/// The Note Docuement which is contained in the Note Editor
		/// </summary>
		private DomDocument document;
		private DomElement paraBlock;
		public KeyboardListener keyboardListener = new KeyboardListener ();
		public DomDocumentListener domDocumentListener = new DomDocumentListener ();

		#endregion fields

		#region Constructors
		
		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowController () : base ("MainWindow")
		{
			Initialize ();

		}
		
		// Shared initialization code
		void Initialize ()
		{
		}
		
		
		#endregion
		
		#region Actions
		partial void NewNoteButton_Clicked (MonoMac.AppKit.NSButton sender)
		{
			NewNote ();
		}
		
		
		#endregion Actions
		
		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}
		
		// This method will be called automatically when the main window "wakes up".
		[Export ("awakeFromNib:")]
		public override void AwakeFromNib ()
		{
			tblNotes.Source = new TableNotesDataSource (tblNotes, searchField, noteWebView);
			TableNotesDataSource.SelectedNoteChanged += delegate(Note note) {
				loadNote (note);
			};

			KeyboardListener.NoteContentChanged += NoteContentChanged;
							
            loadNoteWebKit ();
			setTitle ("Tomboy");
			noteWebView.OnFinishedLoading += delegate {
				installKeyboardHandler ();
				Console.WriteLine ("OnFinishedLoading");
			};
			noteWebView.FinishedLoad += delegate {
				document = noteWebView.MainFrameDocument;
				Console.WriteLine ("webView Finished loading");
			};
			this.Window.WillClose += delegate {
				Console.WriteLine ("Will Close");
			};
		}
		
		#region Private Methods

		/// <summary>
		/// Sets the title in the Note Editor View
		/// </summary>
		/// <param name='title'>
		/// Title.
		/// </param>
		private void setTitle (String title)
		{
			mainWindow.Title = title;
		}

		private void NewNote ()
		{
			Note note = AppDelegate.NoteEngine.NewNote ();
			note.Title = "New Note";
			paraBlock = document.GetElementById("main_content");
			paraBlock.TextContent = "Example Note";
			setTitle (note.Title);
			/* select the row that the new Note was added to.
			 * In TableNotesDataSource we assume and add the new Note to index 0 in the notes arraylist
			 */
			tblNotes.SelectRow (0, false);
			/* Set the new row to the viewable row */
			System.Drawing.PointF point = new System.Drawing.PointF (0, 0);
			notesScrollView.ScrollPoint (point);
			notesScrollView.ContentView.ScrollPoint (point);
			
			/* Example of programmically making element editable 
			* ((DomHtmlElement)document.GetElementById("main_content")).
			*/
		}

		/// <summary>
		/// Handle Note content that changed
		/// </summary>
		private void NoteContentChanged ()
		{

		}

		/// <summary>
		/// Retrieves the active note content
		/// </summary>
		/// <returns>
		/// The note content.
		/// </returns>
		private String ActiveNoteContent ()
		{
			DomElement block = document.GetElementById("main_content");
			return block.InnerText;
		}

		// Install the click handler so we can capture changes to the note
		private void installKeyboardHandler()
		{
			var dom = noteWebView.MainFrameDocument;
			var element = dom.GetElementById ("body");

			element.AddEventListener ("focusout", domDocumentListener, true);
			element.AddEventListener ("blur", domDocumentListener, true);
			element.AddEventListener ("keypress", keyboardListener, true);
		}

		/// <summary>
		/// Loads the note into the WebKit view
		/// </summary>
		/// <param name='note'>
		/// Note.
		/// </param>
		private void loadNote (Note note)
		{
			mainWindow.Title = note.Title;
			paraBlock = document.GetElementById("main_content");
			paraBlock.TextContent = note.Text;
		}
		
		/// <summary>
		/// Loads the note web kit.
		/// </summary>
		private void loadNoteWebKit ()
		{
			// Configure webView to let JavaScript talk to this object.
			noteWebView.WindowScriptObject.SetValueForKey(this, new NSString("MainWindowController"));
			Console.WriteLine ("WindowScriptObject loaded");
			
			/*
			Notes:
				1. In JavaScript, you can now talk to this object using "window.MainWindowController".

				2. You must explicitly allow methods to be called from JavaScript;
				    See the "public static bool IsSelectorExcludedFromWebScript(MonoMac.ObjCRuntime.Selector aSelector)"
				    method below for an example.

				3. The method on this class which we call from JavaScript is showMessage:
				    To call it from JavaScript, we use window.AppController.showMessage_()  <-- NOTE colon becomes underscore!
				    For more on method-name translation, see:
				    http://developer.apple.com/documentation/AppleApplications/Conceptual/SafariJSProgTopics/Tasks/ObjCFromJavaScript.html#
			*/

			// Load the HTML document
			var htmlPath = Path.Combine (NSBundle.MainBundle.ResourcePath, "note.html");
			Console.WriteLine ("htmlPath {0}", htmlPath);
			noteWebView.MainFrame.LoadRequest(new NSUrlRequest (new NSUrl (htmlPath)));
		}
		#endregion Private Methods
	}
}