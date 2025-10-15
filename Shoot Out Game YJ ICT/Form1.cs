// 표준 아이브러리, WinForms, 네트워킹, 스레드 등 필요한 네임스페이스를 불러옴
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using MySql.Data.MySqlClient;
using Microsoft.VisualBasic;




// WinForms 폼 클래스이다.
namespace Shoot_Out_Game_YJ_ICT
{
    public partial class Form1 : Form
    {
        // 상태 변수
        bool goLeft, goRight, goUp, goDown, gameOver; // 키 입력 지속 상태(눌러있는 동안 true)
        string facing = "up"; // facing 플레이어가 바라보는 방향(총알 방향 및 네트워크 전송에 사용)
        int playerHealth = 100; // 체력
        int speed = 10; // 이동속도
        int ammo = 10; // 탄약
        int score; // 점수
        int zombieSpeed = 3; // 좀비속도
        Random randNum = new Random(); // 드랍/스폰 위치 난수

        List<PictureBox> zombiesList = new List<PictureBox>(); // 생성된 좀비들을 추적

        // 서버와의 TCP 연결 및 수신 스레드
        TcpClient client; // TCP 연결을 관리하는 객체
        NetworkStream stream; // Client에서 데이터를 주고받을 수 있는 실제 전송 채널
        Thread recvThread;

        PictureBox otherPlayer = new PictureBox(); // 상대 플레이어를 그릴 pictureBox (멀티플레이 표시용)

        private string connStr = "Server=172.16.0.233;Database=game_rank;Uid=root;Pwd=1234;";


        private void SaveRanking(string nickname, int kills)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "INSERT INTO ranking (nickname, kills) VALUES (@nick, @kills)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@nick", nickname);
                    cmd.Parameters.AddWithValue("@kills", kills);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB 저장 오류: " + ex.Message);
            }
        }


        public Form1(TcpClient c, NetworkStream s, bool isPlayer1)
        {
            // 처음 Form을 만들때 자동으로 생성되는 것이며, this.~ 이렇게 객체를 만들고
            // 객체를 만들고 속성을 설정하고 이벤트를 연결하는 작업을 전부 담당한다.
            InitializeComponent(); // Form1.Designer.cs
            this.Paint += new PaintEventHandler(Form1_Paint); // UI프레임을 그리는 핸들러 추가
            RestartGame(); // 게임 상태 리셋



            client = c;
            stream = s;
            // 플레이어 1,2 시작 위치 설정
            if (isPlayer1)
            {
                player.Left = 100;
                player.Top = 300;
            }
            else
            {
                player.Left = 700;
                player.Top = 300;
            }

            otherPlayer = null; // 아직 상대가 안그려짐

            // 서버 수신 스레드 시작
            recvThread = new Thread(ReceiveData);
            recvThread.IsBackground = true;
            recvThread.Start();
        }

        private void ReceiveData()
        {
            try // 네트워크 I/O 중 예외(연결 끊김 등)가 나올 수 있으니 예외 처리 블록 시작
            {
                byte[] buffer = new byte[1024]; // 수신 바이트를 임시로 담을 1KB 버퍼
                while (true) // 무한 루프 ( 연결 종료 / 예외가 나면 빠져나감)
                {
                    // 반환값 bytes = 실제로 읽힌 바이트 수. 데이터가 올떄까지 대기
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) break; // 0 이면 원격이 정상 종료 --> 루프ㅡ 중단


                    // 방금 읽은 바이트를 UTF-8문자열로 변환
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    string[] data = msg.Split(','); // 받은 문자열을 쉼표로 분리

                    if (data[0] == "POS" && data.Length == 7) // 4개의 문자열을 받는다면
                    {
                        int x = int.Parse(data[1]); // pos 다음 문자열부터 1
                        int y = int.Parse(data[2]); // 2
                        string dir = data[3]; // 3
                        int otherHealth = int.Parse(data[4]);
                        int otherAmmo = int.Parse(data[5]);
                        int otherScore = int.Parse(data[6]);




                        // UI 스레드에서 컨트롤을 건드리기 위해서 Invoke를 사용한다.
                        // WinForms는 UI 스레드가 아닌 곳에서 컨트롤 수정 금지
                        this.Invoke(new Action(() =>

                        {
                            if (otherPlayer == null) // 상대 플레이어가 없으면 동적 생성해서 폼에 추가
                            {
                                otherPlayer = new PictureBox();
                                otherPlayer.SizeMode = PictureBoxSizeMode.AutoSize;
                                otherPlayer.Tag = "player2";
                                this.Controls.Add(otherPlayer); // otherplayer 라는 PictureBox를 이폼에 올려서 화면에 보이게 했다
                            }

                            if (dir == "dead" || otherHealth <= 0)
                            {
                                otherPlayer.Image = Properties.Resources.dead;
                                healthBar2.Value = 0;
                                txtAmmo2.Text = $"Ammo: {otherAmmo}";
                                txtScore2.Text = $"Kills: {otherScore}";
                                return;
                            }


                            // 상대 플레이어의 좌표이다.
                            // 받은 방향 문자열에 맞춰 스프라이트 이미지 갱신
                            otherPlayer.Left = x;
                            otherPlayer.Top = y;

                            switch (dir)
                            {
                                case "up": otherPlayer.Image = Properties.Resources.up; break;
                                case "down": otherPlayer.Image = Properties.Resources.down; break;
                                case "left": otherPlayer.Image = Properties.Resources.left; break;
                                case "right": otherPlayer.Image = Properties.Resources.right; break;
                                case "dead": otherPlayer.Image = Properties.Resources.dead; break;
                            }

                            healthBar2.Text = $"HP: {otherHealth}";
                            txtAmmo2.Text = $"Ammo: {otherAmmo}";
                            txtScore2.Text = $"Kills: {otherScore}";

                        }));
                    }
                    else if (data[0] == "BULLET" && data.Length == 4)
                    {
                        string dir = data[1];
                        int bulletX = int.Parse(data[2]);
                        int bulletY = int.Parse(data[3]);

                        this.Invoke(new Action(() =>
                        {
                            Bullet otherBullet = new Bullet();
                            otherBullet.direction = dir;
                            otherBullet.bulletLeft = bulletX;
                            otherBullet.bulletTop = bulletY;
                            otherBullet.MakeBullet(this);  // 💥 상대방 총알 로컬 생성
                        }));
                    }
                }
            }
            catch
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("서버 연결이 끊어졌습니다.");
                }));
            }
        }

        private void SendPosition()
        {

            // TCP 연결이 없거나 끊어졌을 떄는 함수 종료
            if (client == null || !client.Connected) return;

            // 보낼 데이터를 문자열로 조합
            string msg = $"POS,{player.Left},{player.Top},{facing},{playerHealth},{ammo},{score}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                // 데이터를 서버에 보냄 ( stream 연결통로 )
                stream.Write(data, 0, data.Length);
            }
            catch { }
        }

        private void SendBullet(string direction, int x, int y)
        {
            if (client == null || !client.Connected) return;
            string msg = $"BULLET,{direction},{x},{y}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch { }
        }


        // UI 프레임 ---> 상단에 네모박스 UI player1,2 네모 박스임
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen borderpen = new Pen(Color.White, 2))
            {
                Rectangle player1UI = new Rectangle(10, 10, 450, 40);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(borderpen, player1UI);

                Rectangle player2UI = new Rectangle(480, 10, 450, 40);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(borderpen, player2UI);
            }
        }



        private void MainTimerEvent(object sender, EventArgs e)
        {
            // 체력이 1 이하면 사망처리
            // 게임 오버 처리 되고
            // 상대에게 dead 전송
            if(playerHealth > 1)
            {
                healthBar.Value = playerHealth;
            }
            else
            {
                gameOver = true;
                player.Image = Properties.Resources.dead;
                facing = "dead";
                SendPosition();
                GameTimer.Stop();

                string nick = Microsoft.VisualBasic.Interaction.InputBox("닉네임을 입력하세요", "게임 오버", "Player");

                if (!string.IsNullOrWhiteSpace(nick))
                {
                    SaveRanking(nick, score);
                }


                RankingForm rankForm = new RankingForm();
                rankForm.ShowDialog();

            }

            // UI 텍스트 갱신
            txtAmmo.Text = "Ammo: " + ammo;
            txtScore.Text = "kills: " + score;



            // 이동 처리 ( 벽 충동 포함)
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


            // this(현재 Form1) 에 있는것을 검사
            foreach (Control x in this.Controls)
            {
                // 탄약상자만 골라 처리하는 로직
                if(x is PictureBox && (string)x.Tag == "ammo")
                {
                    // x(ammo) 와 player 두 사각형이 겹치면 true
                    if(player.Bounds.IntersectsWith(x.Bounds))
                    {
                        this.Controls.Remove(x); // 겹치면 x 가 사라짐
                        ((PictureBox)x).Dispose(); // 실제 리소스 해제
                        ammo += 5; // 총알 갯수 추가
                    }
                }

                // zombie 만 골라 처리하는 로직
                if (x is PictureBox && (string) x.Tag == "zombie")
                {
                    //좀비와 플레이어 두 사각형이 겹치면 true
                    if(player.Bounds.IntersectsWith(x.Bounds))
                    {
                        // 체력 -1
                        playerHealth -= 1;
                        SendPosition();
                    }

                    // 좀비 이동//플레이어 데미지
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
                    // 총알과 좀비만 따로 처리하는 로직
                    if (j is PictureBox && (string)j.Tag == "bullet" && x is PictureBox && (string)x.Tag == "zombie")
                    {

                        // 좀비와 총알 두 사각형이 겹쳤을 때 true
                        if(x.Bounds.IntersectsWith(j.Bounds))
                        {

                            // 점수 +
                            score++;
                            SendPosition();


                            this.Controls.Remove(j); // 총알 제거
                            ((PictureBox)j).Dispose(); // 총알 리소스 해제
                            this.Controls.Remove(x); // 좀비 제거
                            ((PictureBox)x).Dispose(); // 좀비 리소스 해제
                            zombiesList.Remove(((PictureBox)x));// 리스트에서 해당 좀비 객체도 제거
                            MakeZombies();// 새로운 좀비 생성
                            
                            if (randNum.Next(0,5) == 0) // 20% 확률 체력 드랍
                            {
                                DropHeart();
                            }

                        }
                    }
                }

                
                foreach ( Control y in this.Controls )
                {
                    // 하트만 처리하는 로직
                    if(y is PictureBox && (string)y.Tag == "heart")
                    {
                        // 플레이어와 하트 두 사각형이 만났을 떄 true
                        if(player.Bounds.IntersectsWith(y.Bounds))
                        {
                            this.Controls.Remove(y);// 하트 제거
                            ((PictureBox)y).Dispose(); // 하트 리소스 해제

                            playerHealth += 20; // 플레이어 체력 +20
                            if (playerHealth > 100) playerHealth = 100; // 100 이상일때는 그대로 100
                        }
                    }
                }



            }
        }


        // 키 눌렀을때
        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            // 게임이 끝났으면 입력 무시
            if(gameOver == true)
            {
                return;
            }

            // 왼쪽방향키 눌렀을때
            if (e.KeyCode == Keys.Left)
            {
                goLeft = true;
                facing = "left";
                player.Image = Properties.Resources.left;
                SendPosition();// 즉시 서버에 방향/위치 전송
            }

            // 오른쪽방향키 눌렀을때
            if (e.KeyCode == Keys.Right)
            {
                goRight = true;
                facing = "right";
                player.Image = Properties.Resources.right;
                SendPosition();// 즉시 서버에 방향/위치 전송
            }

            // 위쪽 방향키 눌렀을때
            if (e.KeyCode == Keys.Up)
            {
                goUp = true;
                facing = "up";
                player.Image = Properties.Resources.up;
                SendPosition();// 즉시 서버에 방향/위치 전송
            }

            // 아래 방향키 눌렀을때
            if (e.KeyCode == Keys.Down)
            {
                goDown = true;
                facing = "down";
                player.Image = Properties.Resources.down;
                SendPosition();// 즉시 서버에 방향/위치 전송
            }
        }


        // 누른 키를 뗸 순간 로직
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


            // 스페이스바 눌렀다 똈을 경우 총알 발사
            if (e.KeyCode == Keys.Space && ammo > 0 && gameOver == false)
            {
                ammo--;
                ShootBullet(facing);
                SendPosition();

                SendBullet(facing, player.Left + (player.Width / 2), player.Top + (player.Height / 2));

                if (ammo < 1)
                {
                    DropAmmo();
                }
            }    

            // 엔터 누르고 뗐을 경우 재시작
            if(e.KeyCode == Keys.Enter && gameOver ==true)
            {
                RestartGame();
            }
        }


        // 총알 생성 로직
        private void ShootBullet(string direction)
        {
            Bullet shootBullet = new Bullet(); // 총알 객체 생성
            shootBullet.direction = direction; // 어떤 방향으로 나가야 하는지 정보 저장
            // 플레이어 중앙에서 나가게 설정
            shootBullet.bulletLeft = player.Left + (player.Width / 2);
            shootBullet.bulletTop = player.Top + (player.Height / 2);
            shootBullet.MakeBullet(this);// 폼위에 표시
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void txtAmmo2_Click(object sender, EventArgs e)
        {

        }

        // 좀비 생성 로직
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


        // 하트 트랍 로직
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

        // Ammo 드랍 로직
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
        

        // 게임 리셋
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
