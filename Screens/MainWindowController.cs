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
            //ApplyFilter();
        };


        textView = new TextView();
        textView.WrapMode = WrapMode.Word; // Set wrap mode to word to wrap long lines
        textView.Editable = false; // Set editable to true if you want to allow editing
        textView.Buffer.Text = "This is a large text box example.\nYou can enter and view large amounts of text here.";

        /*
        Box box = new Box(Orientation.Vertical, 5);
        box.Add(interfaceTreeView);
        box.Add(textView);
        */

        // Create a box for aligning the entry and button horizontally
        var filterBox = new HBox(false, 5);
        filterBox.PackStart(filterEntry, true, true, 0);
        filterBox.PackStart(applyFilterButton, false, false, 0);

        // Create a scrolled window for the interface tree view
        var interfaceScrolledWindow = new ScrolledWindow();
        interfaceScrolledWindow.Add(interfaceTreeView);

        // Calculate the height of the interfaceTreeView
        int interfaceTreeViewHeight = Height / 4;
        interfaceTreeView.SetSizeRequest(-1, interfaceTreeViewHeight);

        // Add interface scrolled window, separator, filter box, and packet scrolled window to the main box
        MainBox.PackStart(interfaceScrolledWindow, true, true, 0);
        MainBox.PackStart(textView, true, true, 0);
        MainBox.PackStart(separator, false, false, 5); // Add padding between interface and filter entry
        MainBox.PackStart(filterBox, false, false, 5); // Add padding between filter entry and packet list
        MainBox.PackStart(packetScrolledWindow, true, true, 0);

        // Calculate the height of the packetTreeView
        int packetTreeViewHeight = Height / 2;
        packetTreeView.SetSizeRequest(-1, packetTreeViewHeight);
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
                Console.WriteLine("Selected interface: " + selectedValue);
                packetStore.Clear();
                StopPacketCaptureAll();
                StartPacketCapture(selectedValue);
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

private void OnPacketSelected(object sender, EventArgs e)
{
    if (sender is TreeSelection selection)
    {
        TreeIter iter;
        if (selection.GetSelected(out iter))
        {
            var selectedId = (string)((ListStore)selection.TreeView.Model).GetValue(iter, 0);
            Console.WriteLine("Selected packet ID: " + selectedId);
            
        
            // Find the packet with the selected ID
            var selectedPacket = packets.FirstOrDefault(p => p.Id.ToString() == selectedId);
            
            if (selectedPacket != null)
            {
                // Set the text of the TextView to the content of the selected packet
                textView.Buffer.Text = selectedPacket.Content;
            }
        }
    }
}



    private void StartPacketCapture(string interfaceName)
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
        device.StartCapture();
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
