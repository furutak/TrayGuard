using System;
using System.Windows.Forms;
using System.Security.Permissions;

namespace TrayGuard
{
    public partial class frmCapacity : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        int capacity;

        // �R���X�g���N�^
        public frmCapacity()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void Form4_Load(object sender, EventArgs e)
        {
            //�t�H�[���̏ꏊ���w��
            this.Left = 450;
            this.Top = 100;
        }

        // �e�t�H�[���ŌĂяo���A�e�t�H�[���̏����A�e�L�X�g�{�b�N�X�֊i�[���Ĉ����p��
        public void updateControls(int cap)
        {
            capacity = cap;
            txtNewCapacity.Text = cap.ToString();
        }

        // �e�t�H�[���ŌĂяo���A�L���p�V�e�B�[��Ԃ�
        public int returnCapacity()
        {
            return capacity;
        }

        // frmModuleInTray ���x��������̃V���A�����icapacity�j��ύX
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtNewCapacity.Text, out capacity) && capacity > 0)
            {
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                Close();
            }
        }

        // frmModuleInTray ���x��������̃V���A�����icapacity�j��ύX
        private void txtNewCapacity_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter) return;
            // �n�j�{�^���̉����Ɠ�������
            btnOK_Click(sender, e);
        }

        //����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const long SC_CLOSE = 0xF060L;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) { return; }
            base.WndProc(ref m);
        }
    }
}