using GameController;
using System;
using World;

namespace SnakeGame;

/// <summary>
/// This class is control the view for the snake game
/// </summary>
public partial class MainPage : ContentPage
{
    Controller controller;
    public MainPage()
    {
        //register all the event handler
        InitializeComponent();
        controller = new Controller();

        controller.Connected += sendPlayerName;
        controller.Error += NetworkErrorHandler;

        graphicsView.Invalidate();

        controller.WorldChanged += ChangeWorld;

        worldPanel.SetWorld(controller.GetWorld());

    }

    /// <summary>
    /// tell the worldPanel to draw
    /// </summary>
    private void ChangeWorld()
    {
        worldPanel.SetWorld(controller.GetWorld());
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Focus on the input box so user can give commands
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// Send the play name to the server
    /// </summary>
    private void sendPlayerName()
    {
        controller.Send(nameText.Text);
    }

    /// <summary>
    /// send the command to the server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")//move up
        {
            controller.Send("{\"moving\":\"up\"}");
        }
        else if (text == "a")//move left
        {
            controller.Send("{\"moving\":\"left\"}");
        }
        else if (text == "s")//move down
        {
            controller.Send("{\"moving\":\"down\"}");
        }
        else if (text == "d")//move right
        {
            controller.Send("{\"moving\":\"right\"}");
        }
        entry.Text = "";//clear input box
    }

    /// <summary>
    /// notice the player there's an error during connection
    /// allow them to reconnect
    /// </summary>
    /// <param name="err"></param>
    private void NetworkErrorHandler(string err)
    {
        Dispatcher.Dispatch(() => DisplayAlert("Error", err, "OK"));

        //enable the server input box
        Dispatcher.Dispatch(
        () =>
        {
            connectButton.IsEnabled = true;
            serverText.IsEnabled = true;
        });
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        //validation of the player's input
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        keyboardHack.Focus();

        // Disable the controls and try to connect
        connectButton.IsEnabled = false;
        serverText.IsEnabled = false;
        nameText.IsEnabled = false;

        //start connect with the server
        controller.Connect(serverText.Text);
    }

    /// <summary>
    /// help menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// about infomation of the game
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    /// <summary>
    /// focust on the input box
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}