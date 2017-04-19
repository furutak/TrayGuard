using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Security.Permissions;
using Npgsql;
using System.Collections;
using System.Reflection;

namespace TrayGuard
{
    public partial class frmPallet : Form
    {
        //�e�t�H�[��frmLogin�ցA�C�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //�f�[�^�O���b�h�r���[�p�{�^��
        DataGridViewButtonColumn openCarton;

        //���̑��񃍁[�J���ϐ�
        DataTable dtPallet;
        string userRole;
        string userId;

        // �R���X�g���N�^
        public frmPallet()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmPallet_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            // �t�H�[���̏ꏊ���w��
            this.Left = 20;
            this.Top = 10;

            dtPallet = new DataTable();
            rounddownDtpHour(ref dtpRegsterDateFrom);
            rounddownDtpHour(ref dtpRegisterDateTo);
            updateDataGridViews(dtPallet, ref dgvPallet, true);

            // �C���{�C�X�ԍ���ҏW�ł���̂́A�Ǘ����[�U�[�̂�
            btnUpdateInvoice.Enabled = userRole == "super" ? true : false;
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V�B�e�t�H�[���ŌĂяo���A�e�t�H�[���̏��������p��
        public void updateControls(string uid, string uname, string udept, string urole)
        {
            userId = uid;
            txtLoginName.Text = uname;
            txtLoginDept.Text = udept;
            userRole = urole;
        }

        // �T�u�v���V�[�W���F�f�[�^�e�[�u���̒�`
        private void defineAndReadDatatable(ref DataTable dt)
        {
            dt.Columns.Add("pallet_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("l_cnt", typeof(int));
            dt.Columns.Add("m_qty", typeof(int));
            dt.Columns.Add("batch", typeof(string));
            dt.Columns.Add("register_date", typeof(DateTime));
            dt.Columns.Add("rg_user", typeof(string));
            dt.Columns.Add("cancel_date", typeof(DateTime));
            dt.Columns.Add("cl_user", typeof(string));
            dt.Columns.Add("invoice_no", typeof(string));
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        public void updateDataGridViews(DataTable dt, ref DataGridView dgv, bool load)
        {
            DateTime registerDateFrom = dtpRegsterDateFrom.Value;
            DateTime registerDateTo = dtpRegisterDateTo.Value.AddDays(1);
            string invoiceNo = txtInvoiceNo.Text;
            string palletId = txtPalletId.Text;
            string lot = txtLot.Text;
            string cartonId = txtCartonId.Text;
            string batch= txtBatch.Text;

            bool b_registerDateFrom = cbxRegisterDateFrom.Checked;
            bool b_registerDateTo = cbxRegisterDateTo.Checked;
            bool b_invoiceNo = cbxInvoiceNo.Checked;
            bool b_palletId = cbxPalletId.Checked;
            bool b_lot = cbxLot.Checked;
            bool b_cartonId = cbxCartonId.Checked;
            bool b_batch = cbxBatch.Checked;
            bool b_multi_lot = cbxMultiLot.Checked;
            bool b_hideCancel = cbxHideCancel.Checked;

            // ���[�U�[���p�b�N�h�c�����������Ƃ��Ďw�肵���ꍇ�́A�ʂ̂r�p�k�����g�p����
            string sqlX = "select pallet_id, lot, l_cnt, m_qty, batch, register_date, rg_user, cancel_date, cl_user, invoice_no from t_pallet " +
                          "where pallet_id in (select pallet_id from t_carton where carton_id like '" + cartonId + "%')";

            // ���[�U�[���I�����������������A�r�p�k���ɔ��f����
            string sql1 = "select pallet_id, lot, l_cnt, m_qty, batch, register_date, rg_user, cancel_date, cl_user, invoice_no from t_pallet where ";

            bool[] cr = {                                     true,
                                                              true,
                          invoiceNo == string.Empty ? false : true,
                          palletId  == string.Empty ? false : true,
                          lot       == string.Empty ? false : true,
                          cartonId    == string.Empty ? false : true,
                          batch     == string.Empty ? false : true,
                                                              true,
                                                              true};

            bool[] ck = { b_registerDateFrom,
                          b_registerDateTo,
                          b_invoiceNo,
                          b_palletId,
                          b_lot,
                          b_cartonId,
                          b_batch,
                          b_multi_lot,
                          b_hideCancel};

            string sql2 = (!(cr[0] && ck[0]) ? string.Empty : "register_date >= '" + registerDateFrom + "' AND ") +
                          (!(cr[1] && ck[1]) ? string.Empty : "register_date < '" + registerDateTo + "' AND ") +
                          (!(cr[2] && ck[2]) ? string.Empty : "invoice_no like '%" + invoiceNo + "%' AND ") +
                          (!(cr[3] && ck[3]) ? string.Empty : "pallet_id like '%" + palletId + "%' AND ") +
                          (!(cr[4] && ck[4]) ? string.Empty : "lot like '%" + lot + "%' AND ") +
                          (!(cr[5] && ck[5]) ? string.Empty : "carton_id like '%" + cartonId + "%' AND ") +
                          (!(cr[6] && ck[6]) ? string.Empty : "batch = '" + batch + "' AND ") +
                          (!(cr[7] && ck[7]) ? string.Empty : "l_cnt >= 2 AND ") +
                          (!(cr[8] && ck[8]) ? string.Empty : "cancel_date is null AND ");

            bool b_all = (cr[0] && ck[0]) || (cr[1] && ck[1]) || (cr[2] && ck[2]) || (cr[3] && ck[3]) || (cr[4] && ck[4]) || (cr[5] && ck[5]) || (cr[6] && ck[6]);

            if (!b_all)
            {
                MessageBox.Show("Please select at least one check box and fill the criteria.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }

            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 5) + " order by pallet_id";
            string sql4 = string.Empty;
            if (cartonId != string.Empty && b_cartonId) sql4 = sqlX;
            else sql4 = sql3;
            System.Diagnostics.Debug.Print(sql4);

            // �r�p�k���ʂ��A�c�s�`�`�s�`�a�k�d�֊i�[
            dt.Clear();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql4, ref dt);

            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv.DataSource = dt;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �O���b�g�r���[�E�[�Ƀ{�^����ǉ��i����̂݁j
            if (load) addButtonsToDataGridView(dgv);

            //�s�w�b�_�[�ɍs�ԍ���\������
            for (int i = 0; i < dgv.Rows.Count; i++) dgv.Rows[i].HeaderCell.Value = (i + 1).ToString();

            //�s�w�b�_�[�̕����������߂���
            dgv.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);

            // ��ԉ��̍s��\������
            if (dgv.Rows.Count != 0) dgv.FirstDisplayedScrollingRowIndex = dgv.Rows.Count - 1;
         }
        
        // �T�u�T�u�v���V�[�W���F�O���b�g�r���[�E�[�Ƀ{�^����ǉ�
        private void addButtonsToDataGridView(DataGridView dgv)
        {
            openCarton = new DataGridViewButtonColumn();
            openCarton.HeaderText = string.Empty;
            openCarton.Text = "Open";
            openCarton.UseColumnTextForButtonValue = true;
            openCarton.Width = 80;
            dgv.Columns.Add(openCarton);
        }

        // �����{�^�������A���ۂ̓O���b�g�r���[�̍X�V�����邾��
        private void btnSearchPack_Click(object sender, EventArgs e)
        {
            updateDataGridViews(dtPallet, ref dgvPallet, false);
        }

        // �O���b�h�r���[��̃{�^���������A���W���[���t�H�[�����{�����[�h�ŊJ���A�f���Q�[�g����
        private void dgvBoxId_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = int.Parse(e.RowIndex.ToString());

            if (dgvPallet.Columns[e.ColumnIndex] == openCarton && currentRow >= 0)
            {
                //����frmModuleInTray ���J����Ă���ꍇ�́A��������悤����
                if (TfGeneral.checkOpenFormExists("frmCartonOnPallet"))
                {
                    MessageBox.Show("Please close the currently open form.", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }

                string palletId = dgvPallet["pallet_id", currentRow].Value.ToString();
                DateTime palletDate = (DateTime)dgvPallet["register_date", currentRow].Value;
                string batch = txtBatch.Text;
                bool canceled = !String.IsNullOrEmpty(dgvPallet["cl_user", currentRow].Value.ToString());
                bool invoiced = !String.IsNullOrEmpty(dgvPallet["invoice_no", currentRow].Value.ToString());

                // ���W���[���e�L�X�g�{�b�N�X����łȂ��A���`�F�b�N�{�b�N�X���I���A���������ʂ��P�s�̏ꍇ�̂݁A�ăv�����g���[�h��L��
                bool reprintMode = (txtCartonId.Text.Length != 0 && cbxCartonId.Checked && dtPallet.Rows.Count == 1);

                frmCartonOnPallet fP = new frmCartonOnPallet();
                //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
                fP.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    updateDataGridViews(dtPallet, ref dgvPallet, false);
                    this.Focus();
                };

                fP.updateControls(palletId, palletDate, userId, txtLoginName.Text, txtLoginDept.Text, userRole, batch, false, false, canceled, invoiced, reprintMode);
                fP.Show();
            }
        }

        // frmTrayInPack ��ǉ����[�h�ŊJ���A�f���Q�[�g����
        private void btnAddBoxId_Click(object sender, EventArgs e)
        {
            if (TfGeneral.checkOpenFormExists("frmCartonOnPallet")) 
            {
                MessageBox.Show("Please close the currently open form.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }

            frmCartonOnPallet fP = new frmCartonOnPallet();
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fP.RefreshEvent += delegate(object sndr, EventArgs excp) 
            {
                updateDataGridViews(dtPallet, ref dgvPallet, false);
                this.Focus(); 
            };

            fP.updateControls(String.Empty, DateTime.Now, userId, txtLoginName.Text, txtLoginDept.Text, userRole, string.Empty, true, false, false, false, false);
            fP.Show();
        }

        //frmPallet�����ہA��\���ɂȂ��Ă���e�t�H�[��frmLogin��\������
        private void frmPallet_FormClosed(object sender, FormClosedEventArgs e)
        {
            //�e�t�H�[��frmLogin�����悤�A�f���Q�[�g�C�x���g�𔭐�������
            this.RefreshEvent(this, new EventArgs());
        }

        // �T�u�T�u�v���V�[�W���F�c�`�s�d�s�h�l�d�o�h�b�j�d�q�̎��Ԉȉ���؂艺����
        private void rounddownDtpHour(ref DateTimePicker dtp)
        {
            DateTime dval = dtp.Value;
            int hour = dval.Hour;
            int minute = dval.Minute;
            int second = dval.Second;
            int millisecond = dval.Millisecond;
            dtp.Value = dval.AddHours(-hour).AddMinutes(-minute).AddSeconds(-second).AddMilliseconds(-millisecond);
        }

        // �f�[�^�O���b�h�r���[�̃_�u���N���b�N���A�f�[�^���G�N�Z���փG�N�X�|�[�g
        private void dgvCarton_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = new DataTable();
            dt = (DataTable)dgvPallet.DataSource;
            ExcelClass xl = new ExcelClass();
            xl.ExportToExcel(dt);
        }

        // �t�H�[��frmTrayInPack���J����Ă��Ȃ����Ƃ��m�F���Ă���A����
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (TfGeneral.checkOpenFormExists("frmCartonOnPallet"))
            {
                MessageBox.Show("Please close the currently open form.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }
            Close();
        }

        // �C���{�C�X�t�B�[���h�̓o�^�ƃL�����Z��
        private void btnUpdateInvoice_Click(object sender, EventArgs e)
        {
            if (dgvPallet.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvPallet.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select only pallet id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
            // �񖼂��������ꍇ�̂݁A�������s��
            if (dgvPallet.Columns[dgvPallet.CurrentCell.ColumnIndex].Name != "pallet_id")
            {
                MessageBox.Show("Please select only pallet id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
       
            // �L�����Z���ς݃J�[�g�����Ȃ����A�m�F����
            foreach (DataGridViewCell cell in dgvPallet.SelectedCells)
            {
                if (dgvPallet["cl_user", cell.RowIndex].Value.ToString() != string.Empty)
                {
                    MessageBox.Show(cell.Value.ToString() + " is canceled already.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }
            }

            // �I���Z����z��Ɋi�[�A����у��b�Z�[�W�p������𐶐����A�C���{�C�X�ꊇ�o�^�r�p�k�R�}���h�����s
            string[] palletlist = { };
            string message = string.Empty;
            int i = 0;
            int topRow = 0;

            foreach (DataGridViewCell cell in dgvPallet.SelectedCells)
            {
                i += 1;
                Array.Resize(ref palletlist, i);
                palletlist[i - 1] = cell.Value.ToString();
                message = message + Environment.NewLine + cell.Value.ToString();
                if (i == 1) topRow = cell.RowIndex;
            }
            
            frmInvoiceNo fI = new frmInvoiceNo();
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fI.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                updateDataGridViews(dtPallet, ref dgvPallet, false);
            };

            fI.updateControls(palletlist, message, dgvPallet["cl_user", topRow].Value.ToString());
            fI.Show();
        }

        // �J�[�g�����x���̈ꊇ�v�����g�A�E�g�i�N���b�N�Łj
        private void btnPrintCarton_Click(object sender, EventArgs e)
        {
            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvPallet.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select only carton id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
            // �񖼂��������ꍇ�̂݁A�������s��
            if (dgvPallet.Columns[dgvPallet.CurrentCell.ColumnIndex].Name != "pallet_id")
            {
                MessageBox.Show("Please select only pallet id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            // �L�����Z���ς݃g���[��I�����Ă���ꍇ�́A������Ȃ�
            foreach (DataGridViewCell cell in dgvPallet.SelectedCells)
            {
                if (dgvPallet["cl_user", cell.RowIndex].Value.ToString() != string.Empty)
                {
                    MessageBox.Show(cell.Value.ToString() + " has been canceled already.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }
            }

            // �I���Z����z��Ɋi�[�A����у��b�Z�[�W�p������𐶐����A�ꊇ�v�����g���s��
            string[] palletlist = { };
            string message = string.Empty;
            int i = 0;
            foreach (DataGridViewCell cell in dgvPallet.SelectedCells)
            {
                if (dgvPallet["cl_user", cell.RowIndex].Value.ToString() == string.Empty)
                {
                    i += 1;
                    Array.Resize(ref palletlist, i);
                    palletlist[i - 1] = cell.Value.ToString();
                    message = message + Environment.NewLine + cell.Value.ToString();
                }
            }
            if (message == string.Empty)
            {
                MessageBox.Show("No carton id was selected.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TfSato tfs = new TfSato();
            for (int j = 0; j < palletlist.Length; j++)
            {
                //tfs.printStart("palletPega", palletlist[j], 0);
            }
        }

        // �J�[�g�����x���̈ꊇ�v�����g�A�E�g�i�G���^�[�L�[�Łj
        private void btnPrintPallet_KeyDown(object sender, KeyEventArgs e)
        {

        }

        // ����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
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