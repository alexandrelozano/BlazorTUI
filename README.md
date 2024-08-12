# BlazorTUI
## Blazor Server Text User Interface

![Sample](https://raw.githubusercontent.com/alexandrelozano/BlazorTUI/master/Resources/sampleapp.gif)

[Nuget](https://www.nuget.org/packages/BlazorTUI)

### Avaliable controls:
- Label
- TextBox
- Button
- CheckBox
- RadioButton
- ListBox
- GridView
- Frame
- Dialog
- Spinner
- ProgressBar
- MessageBox
- TimeBox

You can use Tab key to go to next control, Shift+Tab keys to go to previous control, Space to select and cursors keys.

### Basic instructions

First create a screen, this must have double width than height
```
Screen screen = new Screen(80, 40);
```

On the razor page declare screen tag
```
<BlazorTUI.BlazorTUI screen=@screen></BlazorTUI.BlazorTUI>
```

Then create a top container and assign to the screen
```
Frame frm1 = new Frame("frm1", "FRAME", 13, 4, 56, 29, Frame.BorderStyle.solid, System.Drawing.Color.Cornsilk, System.Drawing.Color.RebeccaPurple);
screen.topContainer.AddContainer(frm1);
```

Now we can add some containers
```
Frame frm2 = new Frame("frm2", "CHILD FRAME", 3, 3, 43, 12, Frame.BorderStyle.line, System.Drawing.Color.Yellow, System.Drawing.Color.Green);
frm1.AddContainer(frm2);
```

And inside every container we can add controls
```
Label lblName = new Label("lblName", "Name:", 2, 2, 15, System.Drawing.Color.Yellow, System.Drawing.Color.Green);
frm2.AddControl(lblName);
TextBox txtName = new TextBox("txtName", "", 2, 3, 15, System.Drawing.Color.Yellow, System.Drawing.Color.Black);
frm2.AddControl(txtName);
```
