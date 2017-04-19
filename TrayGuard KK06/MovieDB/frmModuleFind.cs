using System;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Linq;

namespace TrayGuard
{
    public partial class frmModuleFind : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        // �R���X�g���N�^
        public frmModuleFind()
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

        // �e�t�H�[���ŌĂяo���A�L���p�V�e�B�[��Ԃ�
        public string returnTargetModule()
        {
            return txtModuleId.Text;
        }

        // frmModuleInTray ���x��������̃V���A�����icapacity�j��ύX
        private void btnOK_Click(object sender, EventArgs e)
        {
            //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
            if (txtModuleId.Text == string.Empty) return;
            this.RefreshEvent(this, new EventArgs());
            Close();
        }

        // frmModuleInTray ���x��������̃V���A�����icapacity�j��ύX
        private void txtNewCapacity_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter) return;
            // �n�j�{�^���̉����Ɠ�������
            btnOK_Click(sender, e);
        }
    }
}