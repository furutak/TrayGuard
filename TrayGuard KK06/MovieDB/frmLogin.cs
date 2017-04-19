using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb; 
using System.Security.Permissions;
using System.Reflection;

namespace TrayGuard
{
    public partial class frmLogin : Form
    {
        // �R���X�g���N�^
        public frmLogin()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmLogin_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            cmbUserId.Enabled = false;
            txtPassword.Enabled = false;

            cmbMenu.Items.Add("1. Tray");
            cmbMenu.Items.Add("2. Pack");
            cmbMenu.Items.Add("3. Carton");
            cmbMenu.Items.Add("4. Pallet");
            cmbMenu.SelectedIndex = 0;
        }

        // �����R���{�{�b�N�X�I�����A���[�U�[�h�c�R���{�{�b�N�X�ɂc�a���X�g��ǉ����A���j���[�R���{�{�b�N�X�ɂ����X�g��ǉ�
        private void cmbDept_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbUserId.Text = string.Empty;
            txtUserName.Text = string.Empty;

            // 2016.08.03 FUJIKI MFG �ɂ� PACK ����������
            string[] aryTray = new string[] { "MFG", "QA", "PC" };
            string[] aryPack = new string[] { "MFG", "PC" };
            string[] aryCart = new string[] { "PC" };
            string[] aryPalt = new string[] { "PC" };
            cmbMenu.Items.Clear();
            if (0 <= Array.IndexOf(aryTray, cmbDept.Text)) cmbMenu.Items.Add("1. Tray");
            if (0 <= Array.IndexOf(aryPack, cmbDept.Text)) cmbMenu.Items.Add("2. Pack");
            if (0 <= Array.IndexOf(aryCart, cmbDept.Text)) cmbMenu.Items.Add("3. Carton");
            if (0 <= Array.IndexOf(aryPalt, cmbDept.Text)) cmbMenu.Items.Add("4. Pallet");
            cmbMenu.Enabled = true;
            cmbMenu.SelectedIndex = 0;
            // 2016.08.03 FUJIKI MFG �ɂ� PACK ����������

            TfSQL tf = new TfSQL();
            string sql = "select distinct user_id from t_user where dept ='" + cmbDept.Text + "' order by user_id";
            System.Diagnostics.Debug.Print(sql);
            tf.getComboBoxData(sql, ref cmbUserId);
            cmbUserId.Enabled = true;

            // 2016.08.03 FUJIKI MFG �ɂ� PACK ����������
            cmbMenu.SelectedIndex = (cmbMenu.Enabled ? cmbMenu.SelectedIndex : 1);
            cmbMenu.Enabled = (cmbMenu.Items.Count > 1);
            // if (cmbDept.Text == "PC") cmbMenu.Enabled = true;
            // else { cmbMenu.Enabled = false; cmbMenu.SelectedIndex = 0; }
            // 2016.08.03 FUJIKI MFG �ɂ� PACK ����������
        }

        // ���[�U�[�h�c�R���{�{�b�N�X�I�����A���[�U�[����\�����A�p�X���[�h�e�L�X�g�{�b�N�X�E���O�C���{�^����L���ɂ���
        private void cmbUserId_SelectedIndexChanged(object sender, EventArgs e)
        {
            TfSQL tf = new TfSQL();
            string sql = "select user_name from t_user where user_id = '" + cmbUserId.Text + "'";
            txtUserName.Text = tf.sqlExecuteScalarString(sql);

            txtPassword.Enabled = true;
        }

        // ���O�C���{�^���������̏���
        private void btnLogIn_Click(object sender, EventArgs e)
        {
            string uid = cmbUserId.Text;
            string menu = cmbMenu.Text;
            string dept = cmbDept.Text;

            if (uid == string.Empty || menu == string.Empty) return;

            TfSQL tf = new TfSQL();
            DataTable dt = new DataTable();
            string sql = "select pass, user_name, dept, u_role from t_user where user_id='" + uid + "'";
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
            string pass = dt.Rows[0]["pass"].ToString();
            string uname = dt.Rows[0]["user_name"].ToString();
            string udept = dt.Rows[0]["dept"].ToString();
            string urole = dt.Rows[0]["u_role"].ToString();

            if (pass != txtPassword.Text)
            {
                MessageBox.Show("Password does not match.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (menu == "1. Tray")
            {
                // �q�t�H�[��frmTray��\�����A�f���Q�[�g�C�x���g��ǉ��F 
                frmTray fT = new frmTray();
                fT.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    // �q�t�H�[��frmTray�����ہA���t�H�[����\������
                    this.Visible = true;
                };
                fT.updateControls(uid, uname, udept, urole);
                fT.Show();
                this.txtPassword.Text = string.Empty;
                if (dept != "PC") this.Visible = false;
            }

            if (menu == "2. Pack" && (dept == "PC" || dept == "MFG"))
            {
                // �q�t�H�[��frmTray��\�����A�f���Q�[�g�C�x���g��ǉ��F 
                frmPack fP = new frmPack();
                fP.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    // �q�t�H�[��frmTray�����ہA���t�H�[����\������
                    this.Visible = true;
                };
                fP.updateControls(uid, uname, udept, urole);
                fP.Show();
                this.txtPassword.Text = string.Empty;
                if (dept != "PC") this.Visible = false;
            }

            if (menu == "3. Carton" && dept == "PC")
            {
                // �q�t�H�[��frmTray��\�����A�f���Q�[�g�C�x���g��ǉ��F 
                frmCarton fC = new frmCarton();
                fC.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    // �q�t�H�[��frmTray�����ہA���t�H�[����\������
                    this.Visible = true;
                };
                fC.updateControls(uid, uname, udept, urole);
                fC.Show();
                this.txtPassword.Text = string.Empty;
                if (dept != "PC") this.Visible = false;
            }

            if (menu == "4. Pallet" && dept == "PC")
            {
                // �q�t�H�[��frmTray��\�����A�f���Q�[�g�C�x���g��ǉ��F 
                frmPallet fC = new frmPallet();
                fC.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    // �q�t�H�[��frmTray�����ہA���t�H�[����\������
                    this.Visible = true;
                };
                fC.updateControls(uid, uname, udept, urole);
                fC.Show();
                this.txtPassword.Text = string.Empty;
                if (dept != "PC") this.Visible = false;
            }
        }

        // ���O�C���{�^���̏�ł̃G���^�[�L�[�ŁA���O�C���{�^���̃N���b�N�Ɠ���������s��
        private void btnLogIn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnLogIn_Click(sender, e);
        }

        // �p�X���[�h�e�L�X�g�{�b�N�X��ł̃G���^�[�L�[�ŁA���O�C���{�^���̃N���b�N�Ɠ���������s��
        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnLogIn_Click(sender, e);
        }
    }
}



