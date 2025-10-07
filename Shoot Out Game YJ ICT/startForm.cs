using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;

namespace Shoot_Out_Game_YJ_ICT
{
    public partial class startForm : Form
    {
        TcpClient client;
        NetworkStream stream;
        bool coinInserted = false;

        public startForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += StartForm_KeyDown;

            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblInsertCoin.TextAlign = ContentAlignment.MiddleCenter;
            lblSelect.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void StartForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 스페이스바를 눌러 서버 연결 시도
            if (!coinInserted && e.KeyCode == Keys.Space)
            {
                try
                {
                    client = new TcpClient();
                    client.Connect("211.185.150.248", 9000);
                    stream = client.GetStream();

                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                    // 서버 응답 처리
                    if (msg.StartsWith("REJECT"))
                    {
                        MessageBox.Show(msg.Split(',')[1]); // "게임이 이미 시작되었습니다."
                        client.Close();
                        return;
                    }
                    else if (msg.StartsWith("ROLE"))
                    {
                        int playerNum = int.Parse(msg.Split(',')[1]);
                        MessageBox.Show($"{playerNum}P로 접속되었습니다!");

                        bool isPlayer1 = (playerNum == 1);
                        coinInserted = true;

                        // 즉시 게임 폼으로 이동
                        Form1 game = new Form1(client, stream, isPlayer1);
                        game.Show();
                        this.Hide();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("서버 연결 실패 : " + ex.Message);
                    return;
                }
            }

        }

        private void startForm_Load(object sender, EventArgs e)
        {

        }

    }
}
