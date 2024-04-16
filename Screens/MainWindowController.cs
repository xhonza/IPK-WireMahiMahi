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

    private TreeView interfaceTreeView;
    private TreeView packetTreeView;
    private ListStore packetStore;
    private ScrolledWindow packetScrolledWindow; // Updated

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

        packetScrolledWindow = new ScrolledWindow(); // Updated
        packetScrolledWindow.Add(packetTreeView); // Updated

        MainBox.PackStart(interfaceTreeView, true, true, 0);
        MainBox.PackStart(packetScrolledWindow, true, true, 0); // Updated

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
    TreeViewColumn IdColumn = new TreeViewColumn();
    IdColumn.Title = "Id";
    treeView.AppendColumn(IdColumn);

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
    CellRendererText idCellRendererText = new CellRendererText(); // Renamed variable
    IdColumn.PackStart(idCellRendererText, true);
    IdColumn.AddAttribute(idCellRendererText, "text", 0);

    CellRendererText timeRendererText = new CellRendererText(); // Changed variable name
    timeColumn.PackStart(timeRendererText, true);
    timeColumn.AddAttribute(timeRendererText, "text", 1);

    CellRendererText sourceRendererText = new CellRendererText(); // Changed variable name
    sourceColumn.PackStart(sourceRendererText, true);
    sourceColumn.AddAttribute(sourceRendererText, "text", 2);

    CellRendererText destinationRendererText = new CellRendererText(); // Changed variable name
    destinationColumn.PackStart(destinationRendererText, true);
    destinationColumn.AddAttribute(destinationRendererText, "text", 3);

    CellRendererText protocolRendererText = new CellRendererText(); // Changed variable name
    protocolColumn.PackStart(protocolRendererText, true);
    protocolColumn.AddAttribute(protocolRendererText, "text", 4);

    CellRendererText lengthRendererText = new CellRendererText(); // Changed variable name
    lengthColumn.PackStart(lengthRendererText, true);
    lengthColumn.AddAttribute(lengthRendererText, "text", 5);

    // Create ListStore with appropriate types for each column
    packetStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
    treeView.Model = packetStore;

    return treeView;
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
            var packet = PacketHandler.HandlePacket(e.GetPacket(),Id);
            Id++;
            Console.WriteLine($"What is packet {packet.Id} \n");
            packetStore.AppendValues(packet.Id,packet.Time,packet.Source,packet.Destination,packet.Protocol,packet.Length);
        });
        device.Open(DeviceModes.Promiscuous);
        device.StartCapture();
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
