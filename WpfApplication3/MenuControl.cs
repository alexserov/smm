using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Utils;
using DevExpress.Xpf.WindowsUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplication3 {
    public enum MenuControlState { Collapsed, ExpandedNonCaptured, ExpandedCaptured }
    public static class MenuControlElements {
        public const string PART_AppButton = "PART_AppButton";
        public const string PART_Presenter = "PART_Presenter";
        public const string PART_BackContainer = "PART_BackContainer";
        public const string PART_Back = "PART_Back";
    }
    [ContentProperty("Elements")]
    public class MenuControl : Control {
        protected MenuControlButton PART_AppButton { get; set; }
        protected MenuControlButton PART_Back { get; set; }
        protected NavigationFrame PART_Presenter { get; set; }
        MainMenuControlSubElement MainLevel { get; set; }

        public ObservableCollection<MenuControlElement> Elements {
            get { return MainLevel.Elements; }
        }
        public MenuControlState State {
            get { return (MenuControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }


        public bool IsRootLevel {
            get { return (bool)GetValue(IsRootLevelProperty); }
            set { SetValue(IsRootLevelProperty, value); }
        }


        public static readonly DependencyProperty IsRootLevelProperty =
            DependencyPropertyManager.Register("IsRootLevel", typeof(bool), typeof(MenuControl), new FrameworkPropertyMetadata(true));


        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(MenuControlState), typeof(MenuControl), new PropertyMetadata(MenuControlState.Collapsed, OnStatePropertyChanged));

        static void OnStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((MenuControl)d).OnStateChanged();
        }


        public MenuControl() {
            MainLevel = new MainMenuControlSubElement();
            MainLevel.Glyph = new DXImageGrayscaleExtension() { Image = new DXImageGrayscaleConverter().ConvertFrom("Home_16x16.png") as DXImageInfo }.ProvideValue(null) as ImageSource;
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseOutside);                        
            levels.Push(MainLevel);
            IsRootLevel = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e) {            
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            PART_AppButton = (MenuControlButton)GetTemplateChild(MenuControlElements.PART_AppButton);
            PART_Back = (MenuControlButton)GetTemplateChild(MenuControlElements.PART_Back);
            PART_Presenter = (NavigationFrame)GetTemplateChild(MenuControlElements.PART_Presenter);
            PART_Presenter.Navigated += PART_Presenter_Navigated;
            PART_AppButton.Click += AppButtonClick;
            PART_Back.Click += BackClick;            
            UpdateItemsPresenter();
        }
        
        protected override void OnMouseEnter(MouseEventArgs e) {
            base.OnMouseEnter(e);
            if (State == MenuControlState.Collapsed)
                State = MenuControlState.ExpandedNonCaptured;
        }
        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            if (State == MenuControlState.ExpandedNonCaptured)
                State = MenuControlState.Collapsed;
        }

        void OnMouseOutside(object sender, MouseButtonEventArgs e) {
            State = MenuControlState.Collapsed;
        }

        void BackClick(object sender, RoutedEventArgs e) {
            PopLevel();            
        }
        void AppButtonClick(object sender, RoutedEventArgs e) {
            if (State != MenuControlState.ExpandedCaptured)
                State = MenuControlState.ExpandedCaptured;
            else
                State = MenuControlState.ExpandedNonCaptured;
            ClearLevel();
        }
        public void ButtonClick(object sender, RoutedEventArgs e) {
            State = MenuControlState.ExpandedCaptured;
            var owner = sender as MenuControlButton;
            var but = owner.Owner as MenuControlCommand;
            var sub = owner.Owner as MenuControlSubElement;
            if (but != null) {
                but.Command.Execute(null);
                State = MenuControlState.Collapsed;
                return;
            }
            if (sub != null) {
                PushLevel(sub);
                OnStateChanged();
            }
        }

        Stack<MenuControlSubElement> levels = new Stack<MenuControlSubElement>();
        void PushLevel(MenuControlSubElement sub) {
            if (levels.Peek() == sub)
                return;            
            levels.Push(sub);
            IsRootLevel = levels.Peek() == MainLevel;
            UpdateItemsPresenter();
            UpdateMouseCapture();
        }
        void PopLevel() {
            levels.Pop();
            IsRootLevel = levels.Peek() == MainLevel;
            UpdateItemsPresenter();
            UpdateExpandedState();
        }
        void ClearLevel() {
            if (levels.Peek() == MainLevel)
                return;
            IsRootLevel = true;
            levels.Clear();
            levels.Push(MainLevel);
            UpdateItemsPresenter();
            UpdateExpandedState();
        }
        MenuControlItemsControl source;
        MenuControlItemsControl oldSource;
        void UpdateItemsPresenter() {
            if (source != null)
                oldSource = source;
            source = new MenuControlItemsControl();
            source.Owner = this;
            source.ItemsSource = levels.Peek().Elements;
            PART_Presenter.Source = source;
        }
        void PART_Presenter_Navigated(object sender, DevExpress.Xpf.WindowsUI.Navigation.NavigationEventArgs e) {
            if (oldSource != null) {
                oldSource.Owner = null;
                oldSource.ItemsSource = null;
            }
        }

        void OnStateChanged() {
            if (State == MenuControlState.Collapsed) {
                ClearLevel();
                VisualStateManager.GoToState(this, "Collapsed", false);
            } else {
                VisualStateManager.GoToState(this, "Expanded", false);
            }
            UpdateMouseCapture();
            UpdateExpandedState();            
        }

        private void UpdateMouseCapture() {
            if (State == MenuControlState.ExpandedCaptured) {
                Mouse.Capture(this, CaptureMode.SubTree);
            } else {
                ReleaseMouseCapture();
            }
        }

        void UpdateExpandedState() {
            if (State == MenuControlState.ExpandedCaptured || State == MenuControlState.ExpandedNonCaptured) {
                source.Do(x => x.Expand());
                return;
            }
            source.Do(x=>x.Collapse());
        }
    }

    public class MenuControlItemsControl : ItemsControl {
        public MenuControl Owner { get; set; }        
        protected override bool IsItemItsOwnContainerOverride(object item) {
            return false;
        }
        protected override DependencyObject GetContainerForItemOverride() {
            return new MenuControlButton();
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);
            MenuControlButton mcb = element as MenuControlButton;
            mcb.Owner = item as MenuControlElement;
            mcb.Glyph = mcb.Owner.Glyph;
            mcb.Content = mcb.Owner.Caption;
            mcb.ShowContent = expanded;
            mcb.Click += ButtonClick;
        }
        protected override void ClearContainerForItemOverride(DependencyObject element, object item) {
            base.ClearContainerForItemOverride(element, item);
            MenuControlButton mcb = element as MenuControlButton;
            mcb.Click -= ButtonClick;
        }

        void ButtonClick(object sender, RoutedEventArgs e) {
            Owner.ButtonClick(sender, e);
        }
        public void Expand() {
            SetState(true);
        }
        public void Collapse() {
            SetState(false);
        }
        bool expanded;
        void SetState(bool expanded) {
            this.expanded = true;
            for (int i = 0; i < Items.Count; i++) {
                var container = ItemContainerGenerator.ContainerFromIndex(i) as MenuControlButton;
                if (container == null)
                    continue;
                container.ShowContent = expanded;
            }
        }
    }
    public class MenuControlButton : Button {
        public ImageSource Glyph {
            get { return (ImageSource)GetValue(GlyphProperty); }
            set { SetValue(GlyphProperty, value); }
        }
        public bool ShowContent {
            get { return (bool)GetValue(ShowContentProperty); }
            set { SetValue(ShowContentProperty, value); }
        }
        public MenuControlElement Owner {
            get { return (MenuControlElement)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }
        public bool IsSubMenu {
            get { return (bool)GetValue(IsSubMenuProperty); }
            set { SetValue(IsSubMenuProperty, value); }
        }
        public static readonly DependencyProperty IsSubMenuProperty =
            DependencyPropertyManager.Register("IsSubMenu", typeof(bool), typeof(MenuControlButton), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty OwnerProperty =
            DependencyPropertyManager.Register("Owner", typeof(MenuControlElement), typeof(MenuControlButton), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnOwnerPropertyChanged)));        
        public static readonly DependencyProperty ShowContentProperty =
            DependencyProperty.Register("ShowContent", typeof(bool), typeof(MenuControlButton), new PropertyMetadata(false));
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register("Glyph", typeof(ImageSource), typeof(MenuControlButton), new PropertyMetadata(null));

        protected static void OnOwnerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((MenuControlButton)d).OnOwnerChanged((MenuControlElement)e.OldValue);
        }

        protected virtual void OnOwnerChanged(MenuControlElement oldValue) {
            IsSubMenu = Owner is MenuControlSubElement;
        }
    }


    #region commands
    public abstract class MenuControlElement {
        public ImageSource Glyph { get; set; }
        public string Caption { get; set; }
    }
    public class MenuControlCommand : MenuControlElement {
        public ICommand Command { get; set; }
    }
    [ContentProperty("Elements")]
    public class MenuControlSubElement : MenuControlElement {
        public ObservableCollection<MenuControlElement> Elements { get; set; }

        public MenuControlSubElement() {
            Elements = new ObservableCollection<MenuControlElement>();
        }
    }
    class MainMenuControlSubElement : MenuControlSubElement {
    }

    #endregion

    #region converters
    public class BooleanToVisibilityConverterExtension : MarkupExtension, IValueConverter {

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    #endregion
}