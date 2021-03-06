﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

using System.Windows.Threading;

namespace Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (x, y) => { };
        string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
        string currentLocalFile = "";
        static string meExtension = "med";
        static string meFileFilter = "Math Editor File (*." + meExtension + ")|*." + meExtension;

        string fileVersion = "1.5";

        EditorControl currentEC;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            StatusBarHelper.Init(this);
            characterToolBar.CommandCompleted += (x, y) => { currentEC.Focus(); };
            equationToolBar.CommandCompleted += (x, y) => { currentEC.Focus(); };
            SetTitle();
            AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(MainWindow_MouseDown), true);
            //Task.Factory.StartNew(CheckForUpdate);
            //if (ConfigManager.GetConfigurationValue(KeyName.firstTime) == "true" || ConfigManager.GetConfigurationValue(KeyName.version) != version)
            //{
            //    string successMessage = "";
            //    if (ConfigManager.SetConfigurationValue(KeyName.firstTime, "false") && ConfigManager.SetConfigurationValue(KeyName.version, version))
            //    {
            //        successMessage = "\r\n\r\nThis message will not be shown again.";
            //    }
            //    MessageBox.Show("Thanks for using Math Editor. Math Editor is under constant development and we regularly release better versions of this product." + Environment.NewLine + Environment.NewLine +
            //                    "Please help us by sending your suggestions, feature requests or bug reports using our facebook page or our website (see help)." + Environment.NewLine + Environment.NewLine +
            //                    successMessage, "Important message");
            //}
            UndoManager.CanUndo += (a, b) => { undoButton.IsEnabled = b.ActionPossible; };
            UndoManager.CanRedo += (a, b) => { redoButton.IsEnabled = b.ActionPossible; };
            EquationBase.SelectionAvailable += new EventHandler<EventArgs>(editor_SelectionAvailable);
            EquationBase.SelectionUnavailable += new EventHandler<EventArgs>(editor_SelectionUnavailable);
            underbarToggle.IsChecked = true;
            TextEquation.InputPropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(TextEquation_InputPropertyChanged);
            //currentEC.ZoomChanged += new EventHandler(editor_ZoomChanged);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var strings = Environment.GetCommandLineArgs();
                if (strings.Length > 1)
                {
                    OpenFile(strings[1]);
                }
            }
            catch { }
            var mode = ConfigManager.GetConfigurationValue(KeyName.default_mode);
            var fontName = ConfigManager.GetConfigurationValue(KeyName.default_font);

            var modes = editorModeCombo.Items;
            foreach (ComboBoxItem item in modes)
            {
                if ((string)item.Tag == mode)
                {
                    editorModeCombo.SelectedItem = item;
                }
            }
            var fonts = equationFontCombo.Items;
            foreach (ComboBoxItem item in fonts)
            {
                if ((string)item.Tag == fontName)
                {
                    equationFontCombo.SelectedItem = item;
                }
            }
            ChangeEditorMode();
            ChangeEditorFont();
            currentEC = ((EditorControl)((ScrollViewer)((TabItem)MainTabControl.SelectedItem).Content).Content);
            currentEC.Focus();
        }        

        void editor_SelectionUnavailable(object sender, EventArgs e)
        {
            copyMenuItem.IsEnabled = false;
            cutMenuItem.IsEnabled = false;
            deleteMenuItem.IsEnabled = false;
            cutButton.IsEnabled = false;
            copyButton.IsEnabled = false;
        }

        void editor_SelectionAvailable(object sender, EventArgs e)
        {
            copyMenuItem.IsEnabled = true;
            cutMenuItem.IsEnabled = true;
            deleteMenuItem.IsEnabled = true;
            cutButton.IsEnabled = true;
            copyButton.IsEnabled = true;
        }

        //private Image ConvertImageToGrayScaleImage()
        //{
        //    Image image = new Image();
        //    BitmapImage bmpImage = new BitmapImage();
        //    bmpImage.BeginInit();
        //    bmpImage.UriSource = new Uri("pack://application:,,,/images/gui/redo.png");
        //    bmpImage.EndInit();
        //    if (!undoButton.IsEnabled)
        //    {
        //        FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
        //        grayBitmap.BeginInit();
        //        grayBitmap.Source = bmpImage;
        //        grayBitmap.DestinationFormat = PixelFormats.Gray8;
        //        grayBitmap.EndInit();
        //        image.Source = grayBitmap;
        //    }
        //    return image;
        //}

        /*void CheckForUpdate()
        {
            if (ConfigManager.GetConfigurationValue(KeyName.checkUpdates) == "false")
            {
                return;
            }
            try
            {
                string newVersion = version;
                using (WebClient client = new WebClient())
                {
                    newVersion = client.DownloadString("http://www.mathiversity.com/matheditor/version");
                }
                string[] newParts = newVersion.Split('.');
                string[] currentParts = version.Split('.');
                for (int i = 0; i < newParts.Count(); i++)
                {
                    if (int.Parse(newParts[i]) > int.Parse(currentParts[i]))
                    {
                        if (MessageBox.Show("A new version of Math Editor with enhanced features is available.\r\nWould you like to download the new version?",
                                            "New version available",
                                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Process.Start("http://www.mathiversity.com/Downloads");
                        }
                        break;
                    }
                    else if (int.Parse(newParts[i]) < int.Parse(currentParts[i]))
                    {
                        break;
                    }
                }
            }
            catch { } // hopeless..
        }*/

        public void HandleToolBarCommand(CommandDetails commandDetails)
        {
            if (commandDetails.CommandType == CommandType.CustomMatrix)
            {
                MatrixInputForm inputForm = new MatrixInputForm(((int[])commandDetails.CommandParam)[0], ((int[])commandDetails.CommandParam)[1]);
                inputForm.ProcessRequest += (x, y) =>
                {
                    CommandDetails newCommand = new CommandDetails { CommandType = CommandType.Matrix };
                    newCommand.CommandParam = new int[] { x, y };
                    currentEC.HandleUserCommand(newCommand);
                };
                inputForm.ShowDialog(this);
            }
            else
            {
                currentEC.HandleUserCommand(commandDetails);
                if (commandDetails.CommandType == CommandType.Text)
                {
                    historyToolBar.AddItem(commandDetails.UnicodeString);
                }
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.DirectlyOver != null)
            {
                if (Mouse.DirectlyOver.GetType() == typeof(EditorToolBarButton))
                {
                    return;
                }
                else if (currentEC.IsMouseOver)
                {
                    //editor.HandleMouseDown();
                    currentEC.Focus();
                }
                characterToolBar.HideVisiblePanel();
                equationToolBar.HideVisiblePanel();
            }
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (!currentEC.IsFocused)
            {
                currentEC.Focus();
                //editor.EditorControl_TextInput(null, e);
                currentEC.ConsumeText(e.Text);
                characterToolBar.HideVisiblePanel();
                equationToolBar.HideVisiblePanel();
            }
            currentEC.InvalidateVisual();
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool AnyDirty = false;

            foreach (TabItem ti in MainTabControl.Items)
            {
                if (ti.Content != null && ((EditorControl)((ScrollViewer)ti.Content).Content).Dirty)
                {
                    AnyDirty = true;
                }
            }

            if (AnyDirty)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save the current document before closing?", "Please confirm", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    if (!ProcessFileSave())
                    {
                        e.Cancel = true;
                    }
                }
            }
            historyToolBar.Save();
        }

        private void OpenCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            bool AnyDirty = false;

            foreach (TabItem ti in MainTabControl.Items)
            {
                if (ti.Content != null && ((EditorControl)((ScrollViewer)ti.Content).Content).Dirty)
                {
                    AnyDirty = true;
                }
            }

            if (AnyDirty)
            {
                MessageBoxResult mbResult = MessageBox.Show("Do you want to save the current document before closing?", "Please confirm", MessageBoxButton.YesNoCancel);
                if (mbResult == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (mbResult == MessageBoxResult.Yes)
                {
                    if (!ProcessFileSave())
                    {
                        return;
                    }
                }
            }
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.CheckPathExists = true;
            ofd.Filter = meFileFilter;
            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                OpenFile(ofd.FileName);
            }
        }

        private void OpenFile(string fileName)
        {
            try
            {
                using (Stream stream = File.OpenRead(fileName))
                {
                    MainTabControl.Items.Clear();
                    Stream finstr = null;

                    try {
                        ZipInputStream zipInputStream = new ZipInputStream(stream);
                        ZipEntry zipEntry = zipInputStream.GetNextEntry();
                        MemoryStream outputStream = new MemoryStream();
                        if (zipEntry != null)
                        {
                            byte[] buffer = new byte[4096];
                            StreamUtils.Copy(zipInputStream, outputStream, buffer);
                        }
                        outputStream.Position = 0;
                        finstr = outputStream;
                    }
                    catch{
                        stream.Position = 0;
                        finstr = stream;
                    }

                    XDocument xDoc = XDocument.Load(finstr, LoadOptions.PreserveWhitespace);
                    XElement root = xDoc.Root;

                    XElement formattingElement = root.Element("TextManager");
                    XAttribute fileVersionAttribute = root.Attributes("fileVersion").FirstOrDefault();
                    XAttribute appVersionAttribute = root.Attributes("appVersion").FirstOrDefault();
                    string appVersion = appVersionAttribute != null ? appVersionAttribute.Value : "Unknown";
                    if (fileVersionAttribute == null || fileVersionAttribute.Value != fileVersion)
                    {
                        MessageBox.Show("The file was created by a different version (v." + appVersion + ") of Math Editor and uses a different format." + Environment.NewLine + Environment.NewLine +
                                        "Math Editor will still try to open and convert the file to the current version. The operation may fail. " + Environment.NewLine + Environment.NewLine +
                                        "Please create a backup if you want to keep the original file intact.", "Message");
                    }

                    foreach (XElement rowCont in root.Elements("RowContainer"))
                    {
                        EditorControl newEditorControl = new EditorControl();
                        //newEditorControl.Background = Transparent;
                        newEditorControl.Focusable = true;
                        //newEditorControl.FocusVisualStyle = "{x:Null}";
                        newEditorControl.LoadTab(rowCont, formattingElement);
                        newEditorControl.ZoomChanged += new EventHandler(editor_ZoomChanged);
                        newEditorControl.IsVisibleChanged += new DependencyPropertyChangedEventHandler(IsVisibleChanged);

                        ScrollViewer newScrollViewer = new ScrollViewer();
                        //FocusVisualStyle = "{x:Null}"
                        newScrollViewer.Focusable = true;
                        newScrollViewer.ScrollChanged += scrollViewer_ScrollChanged;
                        newScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        newScrollViewer.Content = newEditorControl;

                        TabItem newTabItem = new TabItem();
                        newTabItem.Content = newScrollViewer;
                        newTabItem.Header = (rowCont.Attribute("Name") != null ? rowCont.Attribute("Name").Value : "Tab " + (MainTabControl.Items.Count + 1));
                        MainTabControl.Items.Add(newTabItem);
                    }

                    TabItem newTI = new TabItem();
                    newTI.Header = "+";
                    MainTabControl.Items.Add(newTI);
                }
                currentLocalFile = fileName;
            }
            catch(Exception e)
            {
                currentLocalFile = "";
                MessageBox.Show("File is corrupt or inaccessible OR it was created by an incompatible version of Math Editor.\n\n"+
                   e.ToString() , "Error");
            }
            SetTitle();
        }

        void SetTitle()
        {
            if (currentLocalFile.Length > 0)
            {
                Title = currentLocalFile + " - Math Editor v." + version;
            }
            else
            {
                Title = "Math Editor v." + version;
            }
        }

        string ShowSaveFileDialog(string extension, string filter)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.DefaultExt = "." + extension;
            sfd.Filter = filter;
            bool? result = sfd.ShowDialog(this);
            if (result == true)
            {
                return Path.GetExtension(sfd.FileName) == "." + extension ? sfd.FileName : sfd.FileName + "." + extension;
            }
            else
            {
                return null;
            }
        }

        private void SaveCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            ProcessFileSave();
        }

        private bool ProcessFileSave()
        {
            if (!File.Exists(currentLocalFile))
            {
                string result = ShowSaveFileDialog(meExtension, meFileFilter);
                if (string.IsNullOrEmpty(result))
                {
                    return false;
                }
                else
                {
                    currentLocalFile = result;
                }
            }
            return SaveFile();
        }

        private bool SaveFile()
        {
            try
            {
                using (Stream stream = File.Open(currentLocalFile, FileMode.Create))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {

                        XDocument xDoc = new XDocument();
                        XElement root = new XElement(GetType().Name); //ActiveChild.Serialize();

                        root.Add(new XAttribute("fileVersion", fileVersion));
                        root.Add(new XAttribute("appVersion", Assembly.GetEntryAssembly().GetName().Version));

                        List<XElement> textManagers = new List<XElement>();
                        List<XElement> conts = new List<XElement>();

                        foreach (TabItem ti in MainTabControl.Items)
                        {
                            if(ti.Content != null)
                            {
                                XElement[] xear = ((EditorControl)((ScrollViewer)ti.Content).Content).GetContentForSaving();

                                textManagers.Add(xear[0]);
                                xear[1].SetAttributeValue("Name", ti.Header);
                                conts.Add(xear[1]);
                            }
                        }

                        //TODO merge for better effect

                        foreach(XElement xe in textManagers)
                        {
                            root.Add(xe);
                        }

                        foreach (XElement xe in conts)
                        {
                            root.Add(xe);
                        }

                        xDoc.Add(root);
                        xDoc.Save(memoryStream);
                        memoryStream.Position = 0;

                        ZipOutputStream zipStream = new ZipOutputStream(stream);
                        zipStream.SetLevel(5); //0-9, 9 being the highest level of compression
                        ZipEntry newEntry = new ZipEntry(System.IO.Path.GetFileNameWithoutExtension(currentLocalFile) + ".xml");
                        newEntry.DateTime = DateTime.Now;
                        zipStream.PutNextEntry(newEntry);
                        StreamUtils.Copy(memoryStream, zipStream, new byte[4096]);
                        zipStream.CloseEntry();
                        zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
                        zipStream.Close();			// Must finish the ZipOutputStream before using outputMemStream.    

                    }
                }

                foreach (TabItem ti in MainTabControl.Items)
                {
                    if (ti.Content != null)
                    {
                        ((EditorControl)((ScrollViewer)ti.Content).Content).Dirty = false;
                    }
                }

                SetTitle();
                return true;
            }
            catch(Exception e)
            {
                MessageBox.Show("File could not be saved. Make sure you have permission to write the file to disk.", "Error");
                currentEC.Dirty = true;
            }
            return false;
        }

        private void SaveAsCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            string result = ShowSaveFileDialog(meExtension, meFileFilter);
            if (!string.IsNullOrEmpty(result))
            {
                currentLocalFile = result;
                SaveFile();
            }
        }

        private void CutCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.Copy(true);
        }

        private void CopyCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.Copy(false);
        }

        private void PasteCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.Paste();
        }

        private void PrintCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void UndoCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.Undo();
        }

        private void RedoCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.Redo();
        }

        private void SelectAllCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            currentEC.SelectAll();
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void exportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string imageType = (string)((Control)sender).Tag ?? "png";
            string fileName = ShowSaveFileDialog(imageType, string.Format("Image File (*.{0})|*.{0}", imageType));
            if (!string.IsNullOrEmpty(fileName))
            {
                string ext = Path.GetExtension(fileName);
                if (ext != "." + imageType)
                    fileName += "." + imageType;
                currentEC.ExportImage(fileName);
            }
        }

        private void showNestingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextEquation.ShowNesting = !TextEquation.ShowNesting;
            if (TextEquation.ShowNesting)
            {
                showNestingMenuItem.Header = "Hide Nesting";
            }
            else
            {
                showNestingMenuItem.Header = "Show Nesting";
            }
            currentEC.InvalidateVisual();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void UnderbarToggleCheckChanged(object sender, RoutedEventArgs e)
        {
            if(MainTabControl.SelectedItem != null)
            {
                currentEC.ShowOverbar(underbarToggle.IsChecked == true);
            }
        }

        private void contentsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://mathiversity.com/MathEditor/Documentation");
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void videoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://youtu.be/_j6R2mv3StQ");
        }

        private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            currentEC.DeleteSelection();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!currentEC.IsFocused)
            {
                Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(delegate ()
                {
                    currentEC.Focus();
                }));

                characterToolBar.HideVisiblePanel();
                equationToolBar.HideVisiblePanel();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (WindowStyle == System.Windows.WindowStyle.None && e.Key == Key.Escape)
            {
                ToggleFullScreen();
            }
        }

        void ToggleFullScreen()
        {
            if (WindowStyle == System.Windows.WindowStyle.None)
            {
                fullScreenMenuItem.Header = "_Full Screen";
                WindowStyle = System.Windows.WindowStyle.ThreeDBorderWindow;
                WindowState = System.Windows.WindowState.Normal;
                exitFullScreenButton.Visibility = System.Windows.Visibility.Collapsed;
                closeApplictionButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                fullScreenMenuItem.Header = "_Normal Screen";
                WindowStyle = System.Windows.WindowStyle.None;
                WindowState = System.Windows.WindowState.Normal; //extral call to be on safe side. windows is funky
                WindowState = System.Windows.WindowState.Maximized;
                exitFullScreenButton.Visibility = System.Windows.Visibility.Visible;
                closeApplictionButton.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void fullScreenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void fbMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.facebook.com/matheditor");
        }

        MenuItem lastZoomMenuItem = null;

        private void ZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            customZoomMenu.Header = "_Custom";
            customZoomMenu.IsChecked = false;
            if (lastZoomMenuItem != null && sender != lastZoomMenuItem)
            {
                lastZoomMenuItem.IsChecked = false;
            }
            lastZoomMenuItem = ((MenuItem)sender);
            lastZoomMenuItem.IsChecked = true;
            string percentage = lastZoomMenuItem.Header as string;
            if (!string.IsNullOrEmpty(percentage))
            {
                currentEC.SetFontSizePercentage(int.Parse(percentage.Replace("%", "")));
            }
        }

        void editor_ZoomChanged(object sender, EventArgs e)
        {
            customZoomMenu.Header = "_Custom";
            customZoomMenu.IsChecked = false;
            if (lastZoomMenuItem != null)
            {
                lastZoomMenuItem.IsChecked = false;
                lastZoomMenuItem = null;
            }
        }

        private void CustomZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window zoomWindow = new CustomZoomWindow(this);
            zoomWindow.Owner = this;
            zoomWindow.Show();
        }

        public void SetFontSizePercentage(int number)
        {
            customZoomMenu.Header = "_Custom (" + number + "%)";
            customZoomMenu.IsChecked = true;
            if (lastZoomMenuItem != null)
            {
                lastZoomMenuItem.IsChecked = false;
                lastZoomMenuItem = null;
            }
            currentEC.SetFontSizePercentage(number);
        }

        private void exitFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        public void SetStatusBarMessage(string message)
        {
            statusBarLeftLabel.Content = message;
        }

        public void ShowCoordinates(string coordinates)
        {
            statusBarRightLabel.Content = coordinates;
        }

        Window symbolWindow = null;
        private void symbolMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (symbolWindow == null)
            {
                symbolWindow = new UnicodeSelectorWindow();
                //symbolWindow.Owner = this;
            }
            symbolWindow.Show();
            symbolWindow.Activate();
        }

        Window codePointWindow = null;
        private void codePointMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (codePointWindow == null)
            {
                codePointWindow = new CodepointWindow();
                //codePointWindow.Owner = this;
            }
            codePointWindow.Show();
            codePointWindow.Activate();
        }

        private void integralItalicCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            EquationRow.UseItalicIntergalOnNew = true;
        }

        private void integralItalicCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            EquationRow.UseItalicIntergalOnNew = false;
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                int pos = MainTabControl.SelectedIndex;
                if (pos > 0 && pos == MainTabControl.Items.Count - 1)
                {
                    var tab = MainTabControl.Items[MainTabControl.Items.Count-1];

                    ((TabItem)MainTabControl.Items[MainTabControl.Items.Count - 1]).Header = "Tab "+ (pos+1);

                    EditorControl newEditorControl = new EditorControl();
                    //newEditorControl.Background = Transparent;
                    newEditorControl.Focusable = true;
                    //newEditorControl.FocusVisualStyle = "{x:Null}";
                    newEditorControl.ZoomChanged += new EventHandler(editor_ZoomChanged);
                    newEditorControl.IsVisibleChanged += new DependencyPropertyChangedEventHandler(IsVisibleChanged);

                    ScrollViewer newScrollViewer = new ScrollViewer();
                    //FocusVisualStyle = "{x:Null}"
                    newScrollViewer.Focusable = true;
                    newScrollViewer.ScrollChanged += scrollViewer_ScrollChanged;
                    newScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    newScrollViewer.Content = newEditorControl;

                    ((TabItem)MainTabControl.Items[MainTabControl.Items.Count - 1]).Content = newScrollViewer;

                    TabItem newTabItem = new TabItem();
                    newTabItem.Header = "+";
                    MainTabControl.Items.Add(newTabItem);
                }

                currentEC = ((EditorControl)((ScrollViewer)((TabItem)MainTabControl.SelectedItem).Content).Content);
            }
        }

        private void IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(delegate ()
                {
                    ((EditorControl)sender).Focus();
                }));
            }
        }

        private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            currentEC.InvalidateVisual();
        }

        public IntPtr Handle
        {
            get { return new System.Windows.Interop.WindowInteropHelper(this).Handle; }
        }

        private void meLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://mathiversity.com/MathEditor");
        }

        private void spiroLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://mathiversity.com/Spirograph");
        }

        public bool InputBold
        {
            get
            {
                return TextEquation.InputBold;
            }
            set
            {
                TextEquation.InputPropertyChanged -= TextEquation_InputPropertyChanged;
                TextEquation.InputBold = value;
                currentEC.ChangeFormat("format", "bold", value);
                TextEquation.InputPropertyChanged += TextEquation_InputPropertyChanged;
            }
        }

        public bool InputItalic
        {
            get
            {
                return TextEquation.InputItalic;
            }
            set
            {
                TextEquation.InputPropertyChanged -= TextEquation_InputPropertyChanged;
                TextEquation.InputItalic = value;
                currentEC.ChangeFormat("format", "italic", value);
                TextEquation.InputPropertyChanged += TextEquation_InputPropertyChanged;
            }
        }

        public bool InputUnderline
        {
            get
            {
                return TextEquation.InputUnderline;
            }
            set
            {
                TextEquation.InputPropertyChanged -= TextEquation_InputPropertyChanged;
                TextEquation.InputUnderline = value;
                currentEC.ChangeFormat("format", "underline", value);
                TextEquation.InputPropertyChanged += TextEquation_InputPropertyChanged;
            }
        }

        void TextEquation_InputPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EditorMode")
            {
                var mode = TextEquation.EditorMode.ToString();
                var t = editorModeCombo.Items;
                foreach (ComboBoxItem item in t)
                {
                    if ((string)item.Tag == mode)
                    {
                        editorModeCombo.SelectedItem = item;
                    }
                }
            }
            else if (e.PropertyName == "FontType")
            {
                string fontName = TextEquation.FontType.ToString();
                var t = equationFontCombo.Items;
                foreach (ComboBoxItem item in t)
                {
                    if ((string)item.Tag == fontName)
                    {
                        equationFontCombo.SelectedItem = item;
                    }
                }

            }
            else
            {
                PropertyChanged(this, new PropertyChangedEventArgs(e.PropertyName));
            }
        }

        private void ChangeEditorFont()
        {
            if (currentEC != null)
            {
                TextEquation.InputPropertyChanged -= TextEquation_InputPropertyChanged;
                TextEquation.FontType = (FontType)Enum.Parse(typeof(FontType), (string)((ComboBoxItem)equationFontCombo.SelectedItem).Tag);
                currentEC.ChangeFormat("font", (string)((ComboBoxItem)equationFontCombo.SelectedItem).Tag, true);
                TextEquation.InputPropertyChanged += TextEquation_InputPropertyChanged;
                currentEC.Focus();
            }
        }

        private void EquationFontCombo_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                ChangeEditorFont();
            }
            catch
            {
                MessageBox.Show("Cannot switch to the selected font", "Unidentified Error");
            }
        }

        private void ChangeEditorMode()
        {
            if (currentEC != null)
            {
                ComboBoxItem item = (ComboBoxItem)editorModeCombo.SelectedItem;
                EditorMode mode = (EditorMode)Enum.Parse(typeof(EditorMode), item.Tag.ToString()); 

                TextEquation.InputPropertyChanged -= TextEquation_InputPropertyChanged;
                TextEquation.EditorMode = (EditorMode)Enum.Parse(typeof(EditorMode), (string)((ComboBoxItem)editorModeCombo.SelectedItem).Tag);
                currentEC.ChangeFormat("mode", mode.ToString().ToLower(), true);
                TextEquation.InputPropertyChanged += TextEquation_InputPropertyChanged;
                currentEC.Focus();
            }
        }

        private void EditorModeCombo_DropDownClosed(object sender, EventArgs e)
        {
            ChangeEditorMode();
        }

        private void mvHelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.MathiVersity.com/MathEditor/Documentation/Online-Storage");
        }   
      

        private void NewCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentEC.Dirty)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save the current document before closing?", "Please confirm", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    if (!ProcessFileSave())
                    {
                        return;
                    }
                }
            }
            currentLocalFile = "";
            SetTitle();
            currentEC.Clear();
        }

        private void settingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.Show();
        }
    }
    public static class StatusBarHelper
    {
        static MainWindow window = null;
        public static void Init(MainWindow _window)
        {
            window = _window;
        }

        public static void PrintStatusMessage(string message)
        {
            window.SetStatusBarMessage(message);
        }

        public static void ShowCoordinates(string message)
        {
            window.ShowCoordinates(message);
        }
    }
}
