//#define SREENSHOT_PAUSE


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Microsoft.Kinect;
using System.IO;

namespace DishBreaker
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class DishBreaker : Microsoft.Xna.Framework.Game
    {
        
        // Game World

#if SREENSHOT_PAUSE

        // Used to pause the screen after a number of skeleton tracked events
        // This is so that I can take screenshots 
        // The trackcount and limit
        int trackCount = 0;
        int trackLimit = 200;

#endif
        HUD hud;

        #region Plates and Sprites

        int collision = 0;
        int score = 0;
        int[] position = new int[2]; //to store position of stock of plates
        Vector2[] windowPosition = new Vector2[4];

        interface ISprite
        {
            void Draw(DishBreaker game);
            void Update(DishBreaker game);
        }

        class Plate : DishBreaker.ISprite
        {
            public Texture2D PlateTexture;
            public Texture2D FallTexture;
            public Texture2D BlastTexture;
            public Texture2D personTexture;
            public Texture2D GameOverTexture;
            public Vector2 gameoverPosition = new Vector2(150,150);
            public Vector2 PlatePosition;
            public Vector2 PlateSpeed;
            public bool Burst = false;
            public bool fall = false;
            public bool blast = false;
            public SoundEffect PlatePopSound;
            public int count = 0;
            public int fallcount = 0;
            public int t,t1,t2;
            public bool person;
            public bool dir;

            static Random rand = new Random();

            public void Draw(DishBreaker game)
            {
                if(game.collision > 2)
                    game.spriteBatch.Draw(GameOverTexture,gameoverPosition, Color.White);
                else if (blast)
                    game.spriteBatch.Draw(BlastTexture, PlatePosition, Color.White);
                else if (Burst)
                    game.spriteBatch.Draw(FallTexture, PlatePosition, Color.White);
                else if (person)
                    game.spriteBatch.Draw(personTexture, PlatePosition, Color.White);
                else
                    game.spriteBatch.Draw(PlateTexture, PlatePosition, Color.White);
            }

            public void Update(DishBreaker game)
            {
                if (count > 120 && !Burst)
                {
                    if (person)
                    {
                        if(blast)
                            game.collision++;
                        person = false;
                        blast = false;
                        
                    }
                    do
                    {
                        t = rand.Next(0, 4);
                    } while (((!dir) && (t == game.position[1])) || (dir && (t == game.position[0])));

                    if (dir)
                    {
                        game.position[1] = t;
                    }
                    else
                    {
                        game.position[0] = t;
                    }
                    t = rand.Next(0, 3);
                    PlatePosition = game.windowPosition[t];
                    count = 0;
                }
                if (fall)
                {

                    if (fallcount > t2)
                    {
                        t1 = rand.Next(1, 2);
                        t2 = (t + 1) * rand.Next(1, 2);
                        fallcount = 0;
                        person = true;
                    }
                    do
                    {
                        t = rand.Next(0, 4);
                    }while (((!dir) && (t == game.position[1])) || (dir && (t == game.position[0])));

                    if (dir)
                    {
                        game.position[1] = t;
                    }
                    else
                    {
                        game.position[0] = t;
                    }

                    PlatePosition = game.windowPosition[t];
                    fall = false;
                    count = 0;
                    fallcount++;
                }
                if (Burst)
                {
                    if (!person)
                    {
                        PlateSpeed.X = 0;
                        PlateSpeed.Y = 10;
                        PlatePosition += PlateSpeed;
                        if (PlatePosition.Y > game.GraphicsDevice.Viewport.Height)
                        {
                            fall = true;
                            Burst = false;
                        }
                        return;
                    }
                    else
                    {
                        blast = true;
                        Burst = false;
                        
                    }
                }

                count++;

               
                if (PlateContains(game.PinVector_r) || PlateContains(game.PinVector_l))
                {
                    if (!person)
                    {
                        PlatePopSound.Play();
                        game.score += 10;
                    }
                    Burst = true;
                   
                    return;
                }


            }


            public bool PlateContains(Vector2 pos)
            {
                if (pos.X < PlatePosition.X) return false;
                if (pos.X > (PlatePosition.X + PlateTexture.Width)) return false;
                if (pos.Y < PlatePosition.Y) return false;
                if (pos.Y > (PlatePosition.Y + PlateTexture.Height)) return false;
                return true;
            }

            public Plate(Texture2D inTexture, Vector2 inPosition, SoundEffect inPop, Texture2D infall, Texture2D inblast, Texture2D inperson,Texture2D ingameover, bool Dir)
            {
                PlateTexture = inTexture;
                PlatePosition = inPosition;
                PlatePopSound = inPop;
                FallTexture = infall;
                BlastTexture = inblast;
                dir = Dir;
                personTexture = inperson;
                GameOverTexture =  ingameover;
            }
        }


        List<ISprite> gameSprites = new List<ISprite>();

        #endregion

        #region Kinect

        KinectSensor myKinect;

        SpriteFont messageFont;

        string errorMessage = "";
        //TransformSmoothParameters parameters;

        protected bool setupKinect()
        {
            // Check to see if a Kinect is available
            if (KinectSensor.KinectSensors.Count == 0)
            {
                errorMessage = "No Kinects detected";
                return false;
            }

            // Get the first Kinect on the computer
            myKinect = KinectSensor.KinectSensors[0];

            /* parameters.Correction= 0.1f;
             parameters.Smoothing = 1.0f;
             parameters.JitterRadius=0.05f;
             parameters.Prediction =0.1f;
             parameters.MaxDeviationRadius = 0.05f;*/

            // Start the Kinect running and select all the streams
            try
            {
                myKinect.SkeletonStream.Enable();
                //myKinect.SkeletonStream.Enable(parameters);
                myKinect.ColorStream.Enable();
                myKinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                myKinect.Start();

            }
            catch
            {
                errorMessage = "Kinect initialise failed";
                return false;
            }

            // connect a handler to the event that fires when new frames are available

            myKinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(myKinect_AllFramesReady);

            return true;
        }

        #endregion

        #region Image Processing

        //Here in this region I also add code to draw skeleton.
        byte[] colorData = null;
        short[] depthData = null;

        Texture2D gameMaskTexture = null;
        Texture2D kinectVideoTexture;
        Rectangle fullScreenRectangle;

        Texture2D gameImageTexture;
        Color[] maskImageColors = null;

        Skeleton[] skeletons = null;
        Skeleton activeSkeleton = null;

        int activeSkeletonNumber;

        void myKinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
#if SREENSHOT_PAUSE
            if (trackCount == trackLimit) return;
#endif

            #region Video image

            // Puts a copy of the video image into the kinect video texture

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;

                if (colorData == null)
                    colorData = new byte[colorFrame.Width * colorFrame.Height * 4];

                colorFrame.CopyPixelDataTo(colorData);

                kinectVideoTexture = new Texture2D(GraphicsDevice, colorFrame.Width, colorFrame.Height);

                Color[] bitmap = new Color[colorFrame.Width * colorFrame.Height];

                int sourceOffset = 0;

                for (int i = 0; i < bitmap.Length; i++)
                {
                    bitmap[i] = new Color(colorData[sourceOffset + 2],
                        colorData[sourceOffset + 1], colorData[sourceOffset], 255);
                    sourceOffset += 4;
                }

                kinectVideoTexture.SetData(bitmap);
            }

            #endregion

            #region Skeleton

            // Finds the currently active skeleton

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);
            }

            activeSkeletonNumber = 0;

            for (int i = 0; i < skeletons.Length; i++)
            {
                if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                {
                    activeSkeletonNumber = i + 1;
                    activeSkeleton = skeletons[i];
                    break;
                }
            }

            #endregion

            #region Depth image

            // Creates a game background image with transparent regions 
            // where the player is displayed

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                // Get the depth data

                if (depthFrame == null) return;

                if (depthData == null)
                    depthData = new short[depthFrame.Width * depthFrame.Height];

                depthFrame.CopyPixelDataTo(depthData);

                // Create the mask from the background image

                gameImageTexture.GetData(maskImageColors);

                if (activeSkeletonNumber != 0)
                {

                    for (int depthPos = 0; depthPos < depthData.Length; depthPos++)
                    {
                        // find a player to mask - split off bottom bits
                        int playerNo = depthData[depthPos] & 0x07;

                        if (playerNo == activeSkeletonNumber)
                        {
                            // We have a player to mask

                            // find the X and Y positions of the depth point
                            int x = depthPos % depthFrame.Width;
                            int y = depthPos / depthFrame.Width;

                            // get the X and Y positions in the video feed
                            ColorImagePoint playerPoint = myKinect.MapDepthToColorImagePoint(
                                DepthImageFormat.Resolution320x240Fps30, x, y, depthData[depthPos], ColorImageFormat.RgbResolution640x480Fps30);

                            // Map the player coordinates into our lower resolution background
                            // Have to do this because the lowest resultion for the color camera is 640x480
                            playerPoint.X /= 2;
                            playerPoint.Y /= 2;


                            // convert this into an offset into the mask color data
                            int gameImagePos = (playerPoint.X + (playerPoint.Y * depthFrame.Width));
                            if (gameImagePos < maskImageColors.Length)
                                // make this point in the mask transparent
                                maskImageColors[gameImagePos] = Color.Chocolate;
                        }
                    }
                }

                gameMaskTexture = new Texture2D(GraphicsDevice, depthFrame.Width, depthFrame.Height);
                gameMaskTexture.SetData(maskImageColors);

            }

            #endregion

        }

        Color boneColor = Color.White;

        Texture2D lineDot;

        void drawLine(Vector2 v1, Vector2 v2, Color col)
        {
            Vector2 origin = new Vector2(0.5f, 0.0f);
            Vector2 diff = v2 - v1;
            float angle;
            Vector2 scale = new Vector2(1.0f, diff.Length() / lineDot.Height);
            angle = (float)(Math.Atan2(diff.Y, diff.X)) - MathHelper.PiOver2;
            spriteBatch.Draw(lineDot, v1, null, col, angle, origin, scale, SpriteEffects.None, 1.0f);
        }

        void drawBone(Joint j1, Joint j2, Color col)
        {
            ColorImagePoint j1P = myKinect.MapSkeletonPointToColor(
                j1.Position,
                ColorImageFormat.RgbResolution640x480Fps30);
            Vector2 j1V = new Vector2(j1P.X, j1P.Y);

            ColorImagePoint j2P = myKinect.MapSkeletonPointToColor(
                j2.Position,
                ColorImageFormat.RgbResolution640x480Fps30);
            Vector2 j2V = new Vector2(j2P.X, j2P.Y);

            drawLine(j1V, j2V, col);
        }

        void drawSkeleton(Skeleton skel, Color col)
        {
            // Spine
            drawBone(skel.Joints[JointType.Head], skel.Joints[JointType.ShoulderCenter], col);
            drawBone(skel.Joints[JointType.ShoulderCenter], skel.Joints[JointType.Spine], col);

            // Left leg
            drawBone(skel.Joints[JointType.Spine], skel.Joints[JointType.HipCenter], col);
            drawBone(skel.Joints[JointType.HipCenter], skel.Joints[JointType.HipLeft], col);
            drawBone(skel.Joints[JointType.HipLeft], skel.Joints[JointType.KneeLeft], col);
            drawBone(skel.Joints[JointType.KneeLeft], skel.Joints[JointType.AnkleLeft], col);
            drawBone(skel.Joints[JointType.AnkleLeft], skel.Joints[JointType.FootLeft], col);

            // Right leg
            drawBone(skel.Joints[JointType.HipCenter], skel.Joints[JointType.HipRight], col);
            drawBone(skel.Joints[JointType.HipRight], skel.Joints[JointType.KneeRight], col);
            drawBone(skel.Joints[JointType.KneeRight], skel.Joints[JointType.AnkleRight], col);
            drawBone(skel.Joints[JointType.AnkleRight], skel.Joints[JointType.FootRight], col);

            // Left arm
            drawBone(skel.Joints[JointType.ShoulderCenter], skel.Joints[JointType.ShoulderLeft], col);
            drawBone(skel.Joints[JointType.ShoulderLeft], skel.Joints[JointType.ElbowLeft], col);
            drawBone(skel.Joints[JointType.ElbowLeft], skel.Joints[JointType.WristLeft], col);
            drawBone(skel.Joints[JointType.WristLeft], skel.Joints[JointType.HandLeft], col);

            // Right arm
            drawBone(skel.Joints[JointType.ShoulderCenter], skel.Joints[JointType.ShoulderRight], col);
            drawBone(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], col);
            drawBone(skel.Joints[JointType.ElbowRight], skel.Joints[JointType.WristRight], col);
            drawBone(skel.Joints[JointType.WristRight], skel.Joints[JointType.HandRight], col);
        }

        #endregion

        #region Pin management

        Texture2D pinTexture;
        Rectangle pinRectangle_r;
        Rectangle pinRectangle_l;

        public int Pin_rX, Pin_rY, Pin_lX, Pin_lY;
        public Vector2 PinVector_r, PinVector_l;

        JointType pinJoint_r = JointType.HandRight;
        JointType pinJoint_l = JointType.HandLeft;

        void updatePin()
        {
            if (activeSkeletonNumber == 0)
            {
                Pin_rX = -100;
                Pin_rY = -100;
                Pin_lX = -100;
                Pin_lY = -100;
            }
            else
            {
                Joint joint_r = activeSkeleton.Joints[pinJoint_r];
                Joint joint_l = activeSkeleton.Joints[pinJoint_l];

                ColorImagePoint pinPoint_r = myKinect.MapSkeletonPointToColor(
                    joint_r.Position,
                    ColorImageFormat.RgbResolution640x480Fps30);
                ColorImagePoint pinPoint_l = myKinect.MapSkeletonPointToColor(
                    joint_l.Position,
                    ColorImageFormat.RgbResolution640x480Fps30);

                Pin_rX = pinPoint_r.X;
                Pin_rY = pinPoint_r.Y;
                Pin_lX = pinPoint_l.X;
                Pin_lY = pinPoint_l.Y;
            }

            PinVector_r.X = Pin_rX;
            PinVector_r.Y = Pin_rY;
            PinVector_l.X = Pin_lX;
            PinVector_l.Y = Pin_lY;

            pinRectangle_r.X = Pin_rX - pinRectangle_r.Width / 2;
            pinRectangle_r.Y = Pin_rY - pinRectangle_r.Height / 2;
            pinRectangle_l.X = Pin_lX - pinRectangle_l.Width / 2;
            pinRectangle_l.Y = Pin_lY - pinRectangle_l.Height / 2;

        }

        #endregion

        Random rand = new Random();

        //int Max_noOfSprites = 4;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public DishBreaker()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Make the screen the same size as the video display output
            graphics.PreferredBackBufferHeight = 480;
            graphics.PreferredBackBufferWidth = 640;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            pinRectangle_r = new Rectangle(0, 0, GraphicsDevice.Viewport.Width / 20, GraphicsDevice.Viewport.Width / 20);
            pinRectangle_l = new Rectangle(0, 0, GraphicsDevice.Viewport.Width / 20, GraphicsDevice.Viewport.Width / 20);

            fullScreenRectangle = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            int temp;
            bool Dir;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Texture2D PlateTexture = Content.Load<Texture2D>("stock");
            Texture2D fallTexture = Content.Load<Texture2D>("falling");
            Texture2D blastTexture = Content.Load<Texture2D>("blast");
            Texture2D pTexture = Content.Load<Texture2D>("person");
            Texture2D gameoverTexture = Content.Load<Texture2D>("gameover");

            SoundEffect PlatePop = Content.Load<SoundEffect>("Break");
            pinTexture = Content.Load<Texture2D>("pin");
            messageFont = Content.Load<SpriteFont>("MessageFont");
            lineDot = Content.Load<Texture2D>("whiteDot");

            gameImageTexture = Content.Load<Texture2D>("DiskBreakerBackground_a");
            maskImageColors = new Color[gameImageTexture.Width * gameImageTexture.Height];

            windowPosition[0] = new Vector2(60,225);
            windowPosition[1] = new Vector2(170, 75);
            windowPosition[2] = new Vector2(375, 75);
            windowPosition[3] = new Vector2(510, 225);


            hud = new HUD();
            hud.Font = Content.Load<SpriteFont>("Arial");

            setupKinect();

            for (int i = 0; i < 2; i++)
            {
                temp = rand.Next(0,3);
                if (i == 1)
                    Dir = true;
                else 
                    Dir = false;

                position[i] = temp;
                Plate c = new Plate(PlateTexture, windowPosition[temp], PlatePop, fallTexture, blastTexture, pTexture,gameoverTexture, Dir);
                

                gameSprites.Add(c);
            }

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
#if SREENSHOT_PAUSE
            if (trackCount == trackLimit) return;
#endif
              
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            updatePin();

            foreach (ISprite sprite in gameSprites)
                sprite.Update(this);
            
            hud.Score = score;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (kinectVideoTexture != null)
                spriteBatch.Draw(kinectVideoTexture, fullScreenRectangle, Color.White);

            if (gameMaskTexture != null)
                spriteBatch.Draw(gameMaskTexture, fullScreenRectangle, Color.White);

            foreach (ISprite sprite in gameSprites)
                sprite.Draw(this);

            /************If you want show skeleton remove this from comment
             if (activeSkeleton != null)
             {
                 drawSkeleton(activeSkeleton, Color.White);
             }*/

            spriteBatch.Draw(pinTexture, pinRectangle_r, Color.DarkKhaki);
            spriteBatch.Draw(pinTexture, pinRectangle_l, Color.DarkKhaki);

            if (errorMessage.Length > 0)
            {
                spriteBatch.DrawString(messageFont, errorMessage, Vector2.Zero, Color.White);
            }

            hud.Draw(spriteBatch);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
