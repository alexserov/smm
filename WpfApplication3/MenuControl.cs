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

namespace WpfApplication3
{
    public enum MenuControlState { Collapsed, ExpandedNonCaptured, ExpandedCaptured }
    public static class MenuControlElements
    {
        public const string PART_AppButton = "PART_AppButton";
        public const string PART_Presenter = "PART_Presenter";
    }
    [ContentProperty("Elements")]
    public class MenuControl : Control
    {
        protected MenuControlButton PART_AppButton { get; set; }
        protected MenuControlItemsControl PART_ItemsControl { get; set; }
        MainMenuControlSubElement MainLevel { get; set; }

        public ObservableCollection<MenuControlElement> Elements
        {
            get { return MainLevel.Elements; }
        }
        public MenuControlState State
        {
            get { return (MenuControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(MenuControlState), typeof(MenuControl), new PropertyMetadata(MenuControlState.Collapsed, OnStatePropertyChanged));

        static void OnStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MenuControl)d).OnStateChanged();
        }
        
                
        public MenuControl()
        {            
            MainLevel = new MainMenuControlSubElement();
            MainLevel.Glyph = new BitmapImage(new Uri("menu-24-32.png", UriKind.RelativeOrAbsolute));
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseOutside);
            levels.Push(MainLevel);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PART_AppButton = (MenuControlButton)GetTemplateChild(MenuControlElements.PART_AppButton);
            PART_ItemsControl = (MenuControlItemsControl)GetTemplateChild(MenuControlElements.PART_Presenter);
            PART_ItemsControl.Owner = this;
            PART_AppButton.Click += AppButtonClick;
            PART_AppButton.Owner = MainLevel;
            PART_AppButton.Glyph = MainLevel.Glyph;
            UpdateItemsPresenter();
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (State == MenuControlState.Collapsed)
                State = MenuControlState.ExpandedNonCaptured;
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (State == MenuControlState.ExpandedNonCaptured)
                State = MenuControlState.Collapsed;
        }

        void OnMouseOutside(object sender, MouseButtonEventArgs e)
        {
            State = MenuControlState.Collapsed;
        }
        void AppButtonClick(object sender, RoutedEventArgs e)
        {
            State = MenuControlState.ExpandedCaptured;
            ClearLevel();
        }
        public void ButtonClick(object sender, RoutedEventArgs e)
        {
            State = MenuControlState.ExpandedCaptured;
            var owner = sender as MenuControlButton;
            var but = owner.Owner as MenuControlCommand;
            var sub = owner.Owner as MenuControlSubElement;
            if (but != null)
            {
                but.Command.Execute(null);
                State = MenuControlState.Collapsed;
                return;
            }
            if (sub != null)
            {
                PushLevel(sub);
                OnStateChanged();
            }
        }

        Stack<MenuControlSubElement> levels = new Stack<MenuControlSubElement>();
        void PushLevel(MenuControlSubElement sub)
        {
            if (levels.Peek() == sub)
                return;
            levels.Push(sub);
            UpdateItemsPresenter();
        }
        void PopLevel()
        {
            levels.Pop();
            UpdateItemsPresenter();
        }
        void ClearLevel()
        {
            if (levels.Peek() == MainLevel)
                return;
            levels.Clear();
            levels.Push(MainLevel);
            UpdateItemsPresenter();
        }

        void UpdateItemsPresenter()
        {
            PART_ItemsControl.ItemsSource = levels.Peek().Elements;            
        }

        void OnStateChanged()
        {
            if (State == MenuControlState.ExpandedCaptured)
            {
                Mouse.Capture(this);
                PART_ItemsControl.Expand();
            }
            else
            {
                if (State == MenuControlState.ExpandedNonCaptured)
                {
                    PART_ItemsControl.Expand();
                }
                else
                {
                    PART_ItemsControl.Collapse();
                    ClearLevel();
                }
                ReleaseMouseCapture();
            }
        }
    }

    public class MenuControlItemsControl : ItemsControl
    {
        public MenuControl Owner { get; set; }
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return false;
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MenuControlButton();
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            MenuControlButton mcb = element as MenuControlButton;
            mcb.Owner = item as MenuControlElement;
            mcb.Glyph = mcb.Owner.Glyph;
            mcb.Content = mcb.Owner.Caption;
            mcb.ShowContent = expanded;
            mcb.Click += ButtonClick;
        }
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            MenuControlButton mcb = element as MenuControlButton;
            mcb.Click -= ButtonClick;
        }

        void ButtonClick(object sender, RoutedEventArgs e)
        {
            Owner.ButtonClick(sender, e);
        }
        public void Expand()
        {
            SetState(true);
        }
        public void Collapse()
        {
            SetState(false);
        }
        bool expanded;
        void SetState(bool expanded)
        {
            this.expanded = true;
            for(int i = 0; i<Items.Count; i++){
                var container = ItemContainerGenerator.ContainerFromIndex(i) as MenuControlButton;
                container.ShowContent = expanded;
            }            
        }
    }
    public class MenuControlButton : Button
    {
        public ImageSource Glyph
        {
            get { return (ImageSource)GetValue(GlyphProperty); }
            set { SetValue(GlyphProperty, value); }
        }        
        public bool ShowContent
        {
            get { return (bool)GetValue(ShowContentProperty); }
            set { SetValue(ShowContentProperty, value); }
        }
        public MenuControlElement Owner
        {
            get { return (MenuControlElement)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }
        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register("Owner", typeof(MenuControlElement), typeof(MenuControlButton), new PropertyMetadata(null));
        public static readonly DependencyProperty ShowContentProperty =
            DependencyProperty.Register("ShowContent", typeof(bool), typeof(MenuControlButton), new PropertyMetadata(false));        
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register("Glyph", typeof(ImageSource), typeof(MenuControlButton), new PropertyMetadata(null));
    } 


#region commands
    public abstract class MenuControlElement
    {
        public ImageSource Glyph { get; set; }
        public string Caption { get; set; }
    }
    public class MenuControlCommand : MenuControlElement
    {
        public ICommand Command { get; set; }
    }
    [ContentProperty("Elements")]
    public class MenuControlSubElement : MenuControlElement
    {
        public ObservableCollection<MenuControlElement> Elements { get; set; }

        public MenuControlSubElement()
        {
            Elements = new ObservableCollection<MenuControlElement>();
        }
    }
    class MainMenuControlSubElement : MenuControlSubElement
    {
    }

#endregion

#region converters
    public class BooleanToVisibilityConverterExtension : MarkupExtension, IValueConverter
    {

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
#endregion
}
