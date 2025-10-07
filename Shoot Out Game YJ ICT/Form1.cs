using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Net.Sockets;
using System.Threading;

namespace Shoot_Out_Game_YJ_ICT
{
    public partial class Form1 : Form
    {
        bool goLeft, goRight, goUp, goDown, gameOver;
        string facing = "up";
        int playerHealth = 100;
        int speed = 10;
        int ammo = 10;
        int score;
        int zombieSpeed = 3;
        Random randNum = new Random();

        List<PictureBox> zombiesList = new List<PictureBox>();

        TcpClient client;
        NetworkStream stream;
        Thread recvThread;

        PictureBox otherPlayer = new PictureBox();


        public Form1(TcpClient c, NetworkStream s, bool isPlayer1)
        {
            InitializeComponent();
            this.Paint += new PaintEventHandler(Form1_Paint);
            RestartGame();

            client = c;
            stream = s;

            otherPlayer.Image = Properties.Resources.left;
            otherPlayer.SizeMode = PictureBoxSizeMode.AutoSize;
            otherPlayer.Tag = isPlayer1 ? "player2" : "player1";
            otherPlayer.Left = 700;
            otherPlayer.Top = 300;
            this.Controls.Add(otherPlayer);

            // 서버 수신 스레드 시작
            recvThread = new Thread(ReceiveData);
            recvThread.IsBackground = true;
            recvThread.Start();
        }

        private void ReceiveData()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    string[] data = msg.Split(',');

                    if (data[0] == "POS" && data.Length == 3)
                    {
                        int x = int.Parse(data[1]);
                        int y = int.Parse(data[2]);
                        string dir = data[3];

                        // 다른 플레이어 위치 이동 (UI 스레드로 실행)
                        this.Invoke(new Action(() =>
                        {
                            otherPlayer.Left = x;
                            otherPlayer.Top = y;
                            switch (dir)
                            {
                                case "up":
                                    otherPlayer.Image = Properties.Resources.up;
                                    break;
                                case "down":
                                    otherPlayer.Image = Properties.Resources.down;
                                    break;
                                case "left":
                                    otherPlayer.Image = Properties.Resources.left;
                                    break;
                                case "right":
                                    otherPlayer.Image = Properties.Resources.right;
                                    break;
                                case "dead":
                                    otherPlayer.Image = Properties.Resources.dead;
                                    break;
                            }
                        }));
                    }
                }
            }
            catch
            {
                // 서버 연결 끊김
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("서버 연결이 끊어졌습니다.");
                }));
            }
        }

        private void SendPosition()
        {
            if (client == null || !client.Connected) return;

            string msg = $"POS,{player.Left},{player.Top},{facing}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch { }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen borderpen = new Pen(Color.White, 2))
            {
                Rectangle player1UI = new Rectangle(10, 10, 450, 40);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(borderpen, player1UI);
            }
        }


        private void MainTimerEvent(object sender, EventArgs e)
        {
            if(playerHealth > 1)
            {
                healthBar.Value = playerHealth;
            }
            else
            {
                gameOver = true;
                player.Image = Properties.Resources.dead;
                GameTimer.Stop();
                facing = "dead";
                SendPosition();
            }

            txtAmmo.Text = "Ammo: " + ammo;
            txtScore.Text = "kills: " + score;

            if(goLeft == true && player.Left > 0)
            {
                player.Left -= speed;
                SendPosition();
            }
            if (goRight == true && player.Left + player.Width < this.ClientSize.Width)
            {
                player.Left += speed;
                SendPosition();
            }
            if (goUp == true && player.Top > 40)
            {
                player.Top -= speed;
                SendPosition();
            }
            if (goDown == true && player.Top + player.Height < this.ClientSize.Height)
            {
                player.Top += speed;
                SendPosition();
            }

            foreach (Control x in this.Controls)
            {
                if(x is PictureBox && (string)x.Tag == "ammo")
                {
                    if(player.Bounds.IntersectsWith(x.Bounds))
                    {
                        this.Controls.Remove(x);
                        ((PictureBox)x).Dispose();
                        ammo += 5;
                    }
                }


                if (x is PictureBox && (string) x.Tag == "zombie")
                {
                    if(player.Bounds.IntersectsWith(x.Bounds))
                    {
                        playerHealth -= 1;
                    }

                    if (x.Left > player.Left)
                    {
                        x.Left -= zombieSpeed;
                        ((PictureBox)x).Image = Properties.Resources.zleft;
                    }
                    if (x.Left < player.Left)
                    {
                        x.Left += zombieSpeed;
                        ((PictureBox)x).Image = Properties.Resources.zright;
                    }
                    if (x.Top > player.Top)
                    {
                        x.Top -= zombieSpeed;
                        ((PictureBox)x).Image = Properties.Resources.zup;
                    }
                    if (x.Top < player.Top)
                    {
                        x.Top += zombieSpeed;
                        ((PictureBox)x).Image = Properties.Resources.zdown;
                    }
                }


                foreach ( Control j in this.Controls )
                {
                    if (j is PictureBox && (string)j.Tag == "bullet" && x is PictureBox && (string)x.Tag == "zombie")
                    {
                        if(x.Bounds.IntersectsWith(j.Bounds))
                        {
                            score++;

                            this.Controls.Remove(j);
                            ((PictureBox)j).Dispose();
                            this.Controls.Remove(x);
                            ((PictureBox)x).Dispose();
                            zombiesList.Remove(((PictureBox)x));
                            MakeZombies();
                            
                            if (randNum.Next(0,5) == 0)
                            {
                                DropHeart();
                            }

                        }
                    }
                }

                foreach ( Control y in this.Controls )
                {
                    if(y is PictureBox && (string)y.Tag == "heart")
                    {
                        if(player.Bounds.IntersectsWith(y.Bounds))
                        {
                            this.Controls.Remove(y);
                            ((PictureBox)y).Dispose();

                            playerHealth += 20;
                            if (playerHealth > 100) playerHealth = 100;
                        }
                    }
                }



            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if(gameOver == true)
            {
                return;
            }

            if (e.KeyCode == Keys.Left)
            {
                goLeft = true;
                facing = "left";
                player.Image = Properties.Resources.left;
            }

            if (e.KeyCode == Keys.Right)
            {
                goRight = true;
                facing = "right";
                player.Image = Properties.Resources.right;
            }

            if (e.KeyCode == Keys.Up)
            {
                goUp = true;
                facing = "up";
                player.Image = Properties.Resources.up;
            }

            if (e.KeyCode == Keys.Down)
            {
                goDown = true;
                facing = "down";
                player.Image = Properties.Resources.down;
            }
        }

        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                goLeft = false;
            }

            if (e.KeyCode == Keys.Right)
            {
                goRight = false;
            }

            if (e.KeyCode == Keys.Up)
            {
                goUp = false;
            }

            if (e.KeyCode == Keys.Down)
            {
                goDown = false;
            }

            if (e.KeyCode == Keys.Space && ammo > 0 && gameOver == false)
            {
                ammo--;
                ShootBullet(facing);

                if(ammo < 1)
                {
                    DropAmmo();
                }
            }    

            if(e.KeyCode == Keys.Enter && gameOver ==true)
            {
                RestartGame();
            }
        }

        private void ShootBullet(string direction)
        {
            Bullet shootBullet = new Bullet();
            shootBullet.direction = direction;
            shootBullet.bulletLeft = player.Left + (player.Width / 2);
            shootBullet.bulletTop = player.Top + (player.Height / 2);
            shootBullet.MakeBullet(this);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void MakeZombies()
        {
            PictureBox zombie = new PictureBox();
            zombie.Tag = "zombie";
            zombie.Image = Properties.Resources.zdown;
            zombie.Left = randNum.Next(0,1000);
            zombie.Top = randNum.Next(0, 1000);
            zombie.SizeMode = PictureBoxSizeMode.AutoSize;

            zombiesList.Add(zombie);
            this.Controls.Add(zombie);
            player.BringToFront();
        }

        private void DropHeart()
        {
            PictureBox heart = new PictureBox();
            heart.Image = Properties.Resources.heart;
            heart.SizeMode = PictureBoxSizeMode.StretchImage;
            heart.Size = new Size(30, 30);
            heart.Left = randNum.Next(10, this.ClientSize.Width - heart.Width);
            heart.Top = randNum.Next(60, this.ClientSize.Height - heart.Height);
            heart.Tag = "heart";
            this.Controls.Add(heart);

            heart.BringToFront();
            player.BringToFront();
        }


        private void DropAmmo()
        {
            PictureBox ammo = new PictureBox();
            ammo.Image = Properties.Resources.ammo_Image;
            ammo.SizeMode = PictureBoxSizeMode.AutoSize;
            ammo.Left = randNum.Next(10, this.ClientSize.Width - ammo.Width);
            ammo.Top = randNum.Next(60, this.ClientSize.Height - ammo.Height);
            ammo.Tag = "ammo";
            this.Controls.Add(ammo);

            ammo.BringToFront() ;
            player.BringToFront();
        }
        

        private void RestartGame()
        {
            player.Image = Properties.Resources.up;

            foreach (PictureBox x in zombiesList)
            {
                this.Controls.Remove(x);
            }

            zombiesList.Clear();

            for (int i = 0; i < 3; i++)
            {
                MakeZombies();
            }
            
            goUp = false;
            goDown = false;
            goLeft = false;
            goRight = false;
            gameOver =false;

            playerHealth = 100;
            score = 0;
            ammo = 10;

            GameTimer.Start();
        }
    }
}
