The Pie Menu
------------


Step 1: Assign a PieMenu component to a game object. The gameobject should have
a collider attached. If not, the pie menu can be activated using the 
PieMenuManager.Instance.Show(PieMenu menu) call from your own script.

Step 2: Assign Texture2D instances to the PieMenu icons variable. These images
will be used when the pie menu is activated. 

Step 3: If required, assign a skin to the pie menu. A sample skin is provided in
the Skins folder. The pie menu uses the button style from the skin.

Step 4: Assign command names to the pie menu commands array. These commands are
sent to an OnSelect(string command) method in your components when a pie menu 
option is clicked.
