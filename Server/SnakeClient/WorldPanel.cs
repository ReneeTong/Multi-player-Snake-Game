using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using World;
using System.ComponentModel.DataAnnotations;
using Microsoft.Maui.Graphics;

namespace SnakeGame;
/// <summary>
/// This method draw all the UI
/// </summary>
public class WorldPanel : IDrawable
{
    private IImage wall;
    private IImage background;
    private IImage explosion;

    private bool initializedForDrawing = false;
    private SnakeWorld world;
    private int worldSize;
    private int viewSize;
    private GraphicsView graphicsView = new();

    // A delegate for DrawObjectWithTransform
    // Methods matching this delegate can draw whatever they want onto the canvas
    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private Color[] snakeColors;

#if MACCATALYST
    private IImage loadImage(string name)
    {
        //test git change
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#endif
    /// <summary>
    /// initialize the world view
    /// </summary>
    public WorldPanel()
    {
        graphicsView.Drawable = this;
        worldSize = 2000;
        viewSize = 900;

        // all the posible player's color, beside current player
        snakeColors = new Color[] { Colors.Aquamarine, Colors.Coral, Colors.LightSeaGreen, Colors.OrangeRed, Colors.Honeydew, Colors.Tomato, Colors.LavenderBlush, Colors.OldLace};

    }

    //get the world model
    public void SetWorld(SnakeWorld w)
    {
        world = w;
    }

    /// <summary>
    /// load all the images
    /// </summary>
    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png");
        background = loadImage("Background.png");
        explosion = loadImage("Explosion.png");
        initializedForDrawing = true;
    }

    /// <summary>
    /// draw everything in the view
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        //only draw when the current play is in the fame
        if (world.Snakes.ContainsKey(world.getPlayerID()))
        {
            //prevent race condition with the controller
            lock (world)
            {
                worldSize = world.getWorldSize();
                if (!initializedForDrawing)
                    InitializeDrawing();
                
                //focusing camera on player
                int playerId = world.getPlayerID();
                Snake player = world.Snakes[playerId];
                float playerX = (float)player.body[player.body.Count - 1].X;
                float playerY = (float)player.body[player.body.Count - 1].Y;

                canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));

                // undo previous transformations from last frame
                canvas.ResetState();

                //draw the background
                canvas.DrawImage(background, (-worldSize / 2), (-worldSize / 2), worldSize, worldSize);

                //draw all the snakes
                foreach (Snake s in world.Snakes.Values)
                {
                    //remove died snakes from the view
                    if (s.dc)
                    {
                        world.Snakes.Remove(s.snake);
                    }
                    if (!player.alive)//explosion
                    {
                        canvas.DrawImage(explosion, (float)player.body[player.body.Count - 1].X-40, (float)player.body[player.body.Count - 1].Y-40, 80, 80);
                    }
                    //draw all the alived snake
                    if (!s.died)
                    {
                        //draw the snake
                        SnakeDrawer(s, canvas);

                        //draw the name and the score for snakes
                        canvas.FontColor = Colors.White;
                        canvas.FontSize = 10;
                        canvas.DrawString(s.name, (float)s.body[s.body.Count - 1].X, (float)s.body[s.body.Count - 1].Y, 100, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
                        canvas.DrawString("score: "+ s.score, (float)s.body[s.body.Count - 1].X + 10, (float)s.body[s.body.Count - 1].Y+10, 100, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
                    }
                }

                //draw all the walls
                foreach (Walls w in world.Walls.Values)
                {
                    WallDrawer(w, canvas);
                }

                //draw all the powerups
                foreach (Powerup p in world.Pow.Values)
                {
                    if (!p.died)
                    {
                        PowerUpDrawer(p, canvas);
                    }
                  
                }
            }
        }

    }

    /// <summary>
    /// draw a snake
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void SnakeDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;
        //loop through all the coordinates of the snake body and connect them with lines
        for (int i = 0, j = 1; j < s.body.Count; i++, j++)
        {
            //set the player's snake to pink
            if(s.snake == world.getPlayerID())
            {
                canvas.StrokeColor = Colors.MediumVioletRed;
            }
            else//pick a color for other snakes based on their id
            {
                canvas.StrokeColor = snakeColors[s.snake % 8];
            }
            //draw snakes
            canvas.StrokeSize = 10;
            canvas.DrawLine((float)s.body[i].X, (float)s.body[i].Y, (float)s.body[j].X, (float)s.body[j].Y);
        } 
    }

    /// <summary>
    /// draw powerup
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void PowerUpDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 16;
        //power up has two colors
        if (p.power % 2 == 0)
            canvas.FillColor = Colors.Pink;
        else
            canvas.FillColor = Colors.Green;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillEllipse((float)p.loc.X-width/2, (float)p.loc.Y-width/2, width, width);
    }

    /// <summary>
    /// draw the wall
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        //p1 left endpoint
        //p2 right enpoint
        //each wall image has a width of 50
        Walls w = o as Walls;

        //if two x are the same, it's a vertical line
        //then we want to make sure p2.y > p1.y

        //if two y are the same, it's a horizontal line
        //then we want to make sure p1.x > p2.x

        if (w.p1.X == w.p2.X)//it's a vertical line
        {
            if (w.p2.Y < w.p1.Y)//make sure p1.Y is always the smaller one
            {
                double temp = w.p1.Y;
                w.p1.Y = w.p2.Y;
                w.p2.Y = temp;
            }
            //draw from bottom to top
            double y = w.p1.Y;
            double numberofwallImages = (w.p2.Y - w.p1.Y) / 50;
            for (int i = 0; i <= numberofwallImages; i++)
            {
                canvas.DrawImage(wall, (float)w.p1.X-25, (float)y-25, 50, 50);
                y += 50;
            }
        }
        else//it's a horizontal line
        {
            if (w.p2.X < w.p1.X)//make sure p1.X is always the smaller one
            {
                double temp = w.p1.X;
                w.p1.X = w.p2.X;
                w.p2.X = temp;
            }
            //draw from left to right
            double x = w.p1.X;
            double numberofwallImages = (w.p2.X - w.p1.X) / 50;
            for (int i = 0; i <= numberofwallImages; i++)
            {
                canvas.DrawImage(wall, (float)x-25, (float)w.p1.Y-25, 50, 50);
                x += 50;
            }
        }
    }
}
