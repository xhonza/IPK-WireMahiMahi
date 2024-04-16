using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

public class MainWindowController
{
    public int Id {get; set;}
    public VBox MainBox { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Filter { get; set; }

    public List<Packet> packets = new List<Packet>();

    private TreeView interfaceTreeView;
    private TreeView packetTreeView;
    private ListStore packetStore;
    private ScrolledWindow packetScrolledWindow; // Updated
    private TextView textView;
    private TextView descriptionView;


public MainWindowController()
{
    // Read configuration
    var config = JObject.Parse(File.ReadAllText("Settings/AppConfig.json"));
    Width = (int)config["Window"]["Width"];
    Height = (int)config["Window"]["Height"];

    MainBox = new VBox(false, 5);
    Id = 1;

    interfaceTreeView = CreateInterfaceTreeView();
    packetTreeView = CreatePacketTreeView();

    // Create a vertical separator
    var separator = new VSeparator();

    // Create a scrolled window for the packet tree view
    packetScrolledWindow = new ScrolledWindow();
    packetScrolledWindow.Add(packetTreeView);

    // Create entry for filter input
    var filterEntry = new Entry();
    filterEntry.PlaceholderText = "Filter by...";

    // Create button for applying filter
    var applyFilterButton = new Button("Apply Filter");
    applyFilterButton.Clicked += (sender, args) =>
    {
        Filter = filterEntry.Text;
        ApplyFilter(Filter);
    };


    textView = new TextView();
    textView.WrapMode = WrapMode.Word; // Set wrap mode to word to wrap long lines
    textView.Editable = false; // Set editable to true if you want to allow editing
    textView.Buffer.Text = "";

    descriptionView = new TextView();
    descriptionView.WrapMode = WrapMode.Word; // Set wrap mode to word to wrap long lines
    descriptionView.Editable = false; // Set editable to true if you want to allow editing
    descriptionView.Buffer.Text = "";

    // Create a box for aligning the entry and button horizontally
    var filterBox = new HBox(false, 5);
    filterBox.PackStart(filterEntry, true, true, 0);
    filterBox.PackStart(applyFilterButton, false, false, 0);

    // Create a scrolled window for the interface tree view
    var interfaceScrolledWindow = new ScrolledWindow();
    interfaceScrolledWindow.Add(interfaceTreeView);

    
    Box boxBottom = new HBox();
    
    var contentScrolledWindow = new ScrolledWindow();
    var desScrolledWindow = new ScrolledWindow();
    contentScrolledWindow.Add(textView);
    desScrolledWindow.Add(descriptionView);

    boxBottom.PackStart(desScrolledWindow, true, true, 20);
    boxBottom.PackEnd(contentScrolledWindow, true, true, 5);
    

    // Add interface scrolled window, separator, filter box, and packet scrolled window to the main box
    MainBox.PackStart(interfaceScrolledWindow, false, true, 0);
    MainBox.PackStart(separator, false, false, 5); // Add padding between interface and filter entry
    MainBox.PackStart(filterBox, false, false, 5); // Add padding between filter entry and packet list
    MainBox.PackStart(packetScrolledWindow, true, true, 0);
    MainBox.PackStart(boxBottom, true, true, 5);

    // Calculate the height of the packetTreeView
    int packetTreeViewHeight = Height / 3;
    packetTreeView.SetSizeRequest(-1, packetTreeViewHeight);
}
    private void ApplyFilter(string filter){
        Filter = filter;
        clearCache();
        StopPacketCaptureAll();
        StartPacketCapture(GetSelectedInterface(),true);
    }

    private void ShowNotification(string title, string message)
{
    var dialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
    dialog.Title = title;
    dialog.Run();
    dialog.Destroy();
}

    private TreeView CreateInterfaceTreeView()
    {
        TreeView treeView = new TreeView();
        treeView.HeadersVisible = true;
        TreeViewColumn column = new TreeViewColumn();
        column.Title = "Interfaces";

        CellRendererText cellRenderer = new CellRendererText();
        column.PackStart(cellRenderer, true);
        column.AddAttribute(cellRenderer, "text", 0);
        treeView.AppendColumn(column);

        ListStore store = new ListStore(typeof(string));
        treeView.Model = store;

        foreach (var iface in Interfaces.ListActiveInterfaces())
        {
            store.AppendValues(iface);
        }

        treeView.Selection.Changed += OnInterfaceSelected;

        return treeView;
    }

    private void OnInterfaceSelected(object sender, EventArgs e)
    {
        if (sender is TreeSelection selection)
        {
            TreeIter iter;
            if (selection.GetSelected(out iter))
            {
                var selectedValue = (string)((ListStore)selection.TreeView.Model).GetValue(iter, 0);
                clearCache();
                StopPacketCaptureAll();
                StartPacketCapture(selectedValue,false);
            }
        }
    }

    private void StopPacketCaptureAll()
    {
        var devices = CaptureDeviceList.Instance;
        foreach (var device in devices)
        {
            if (device != null)
            {
                device.StopCapture();
                device.Close();
            }
        }
    }

    private string GetSelectedInterface()
{
    var selection = interfaceTreeView.Selection;
    if (selection != null)
    {
        TreeIter iter;
        if (selection.GetSelected(out iter))
        {
            return (string)((ListStore)selection.TreeView.Model).GetValue(iter, 0);
        }
    }
    return null; // Return null if no item is selected
}
 private TreeView CreatePacketTreeView()
{
    TreeView treeView = new TreeView();
    treeView.HeadersVisible = true;

    // Create columns
    TreeViewColumn idColumn = new TreeViewColumn();
    idColumn.Title = "Id";
    treeView.AppendColumn(idColumn);

    TreeViewColumn timeColumn = new TreeViewColumn();
    timeColumn.Title = "Time";
    treeView.AppendColumn(timeColumn);

    TreeViewColumn sourceColumn = new TreeViewColumn();
    sourceColumn.Title = "Source";
    treeView.AppendColumn(sourceColumn);

    TreeViewColumn destinationColumn = new TreeViewColumn();
    destinationColumn.Title = "Destination";
    treeView.AppendColumn(destinationColumn);

    TreeViewColumn protocolColumn = new TreeViewColumn();
    protocolColumn.Title = "Protocol";
    treeView.AppendColumn(protocolColumn);

    TreeViewColumn lengthColumn = new TreeViewColumn();
    lengthColumn.Title = "Length";
    treeView.AppendColumn(lengthColumn);

    // Add cell renderers to columns
    CellRendererText idRendererText = new CellRendererText();
    idColumn.PackStart(idRendererText, true);
    idColumn.AddAttribute(idRendererText, "text", 0);

    CellRendererText timeRendererText = new CellRendererText();
    timeColumn.PackStart(timeRendererText, true);
    timeColumn.AddAttribute(timeRendererText, "text", 1);

    CellRendererText sourceRendererText = new CellRendererText();
    sourceColumn.PackStart(sourceRendererText, true);
    sourceColumn.AddAttribute(sourceRendererText, "text", 2);

    CellRendererText destinationRendererText = new CellRendererText();
    destinationColumn.PackStart(destinationRendererText, true);
    destinationColumn.AddAttribute(destinationRendererText, "text", 3);

    CellRendererText protocolRendererText = new CellRendererText();
    protocolColumn.PackStart(protocolRendererText, true);
    protocolColumn.AddAttribute(protocolRendererText, "text", 4);

    CellRendererText lengthRendererText = new CellRendererText();
    lengthColumn.PackStart(lengthRendererText, true);
    lengthColumn.AddAttribute(lengthRendererText, "text", 5);

    // Create ListStore with appropriate types for each column
    packetStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
    treeView.Model = packetStore;

    // Add selection changed event handler
    treeView.Selection.Changed += OnPacketSelected;

    return treeView;
}

private void clearCache(){
    packets.Clear();
    packetStore.Clear();
    textView.Buffer.Text = "";
    descriptionView.Buffer.Text = "";
}

private void OnPacketSelected(object sender, EventArgs e)
{
    if (sender is TreeSelection selection)
    {
        TreeIter iter;
        if (selection.GetSelected(out iter))
        {
            var selectedId = (string)((ListStore)selection.TreeView.Model).GetValue(iter, 0);

            // Find the packet with the selected ID
            var selectedPacket = packets.FirstOrDefault(p => p.Id.ToString() == selectedId);

            if (selectedPacket != null)
            {
                // Check if selected packet content is null before setting it to TextView
                if (selectedPacket.Content != null)
                {
                    textView.Buffer.Text = selectedPacket.Content;
                    descriptionView.Buffer.Text = $"\n{selectedPacket.Description}";
                }
                else
                {
                    textView.Buffer.Text = "No content available for this packet.";
                    
                }
            }
        }
    }
}




    private void StartPacketCapture(string interfaceName,bool update)
    {
        var devices = CaptureDeviceList.Instance;
        var device = interfaceName != null ? devices.FirstOrDefault(d => d.Name == interfaceName) : devices.FirstOrDefault();
        if (device == null)
        {
            Console.Error.WriteLine("Cannot open interface for listening");
            return;
        }

        device.OnPacketArrival += new PacketArrivalEventHandler((sender, e) =>
        {
            var packet = PacketHandler.HandlePacket(e.GetPacket(),Id.ToString());
            Id++;
            appendPacket(packet);
            packetStore.AppendValues(packet.Id.ToString(),packet.Time,packet.Source,packet.Destination,packet.Protocol,packet.Length);
        });
        device.Open(DeviceModes.Promiscuous);
        
        try
        {
        device.StartCapture();
        device.Filter = Filter;
        if(update)
            ShowNotification("Filter Applied", "Filter has been successfully applied.");
        }catch(Exception e){
            if(update)
                ShowNotification("Error", "Invalid format of filter.");
            else
                ShowNotification("Error", "Error occured while trying to capture on selected interface.");
        }
    }

    private void appendPacket(Packet packet){
        packets.Add(packet);
    }

    private class Interfaces
    {
        public static List<string> ListActiveInterfaces()
        {
            List<string> interfaceNames = new List<string>();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                interfaceNames.Add(nic.Name);
            }
            return interfaceNames;
        }
    }
}
