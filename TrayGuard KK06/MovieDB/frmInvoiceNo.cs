using System;
using System.Windows.Forms;
using System.Security.Permissions;

namespace TrayGuard
{
    public partial class frmInvoiceNo : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        string[] cartonlist;
        string message;


        // �R���X�g���N�^
        public frmInvoiceNo()
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
        public void updateControls(string[] carton, string msg, string invoice)
        {
            cartonlist = carton;
            message = msg;
            txtInvoiceNo.Text = invoice;
        }

        // frmPallet �C���{�C�X�ԍ���o�^
        private void btnOK_Click(object sender, EventArgs e)
        {
            string invoice = txtInvoiceNo.Text;

            if (invoice == string.Empty)
            {
                DialogResult result = MessageBox.Show("Selected cells' invoice no is to be updated." + Environment.NewLine +
                    "Is it OK?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No) return;
            }

            TfSQL tf = new TfSQL();
            bool res = tf.sqlMultipleUpdateInvoiceOnPallet(cartonlist, invoice);

            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                MessageBox.Show("The following carton IDs' invoice number were updated: " + Environment.NewLine + message, "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
            }
            else
            {
                MessageBox.Show("Invoice number registration was not successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Close();
        }

        // frmModuleInTray ���x��������̃V���A�����icapacity�j��ύX
        private void txtNewCapacity_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�X���̏ꍇ�̂݁A�������s��
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