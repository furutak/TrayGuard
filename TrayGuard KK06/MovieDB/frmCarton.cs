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
    public partial class frmCarton : Form
    {
        //�e�t�H�[��frmLogin�ցA�C�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //�f�[�^�O���b�h�r���[�p�{�^��
        DataGridViewButtonColumn openCarton;

        //���̑��񃍁[�J���ϐ�
        DataTable dtCarton;
        string userRole;
        string userId;

        // �R���X�g���N�^
        public frmCarton()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmCarton_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            // �t�H�[���̏ꏊ���w��
            this.Left = 20;
            this.Top = 10;

            dtCarton = new DataTable();
            rounddownDtpHour(ref dtpRegsterDateFrom);
            rounddownDtpHour(ref dtpRegisterDateTo);
            updateDataGridViews(dtCarton, ref dgvCarton, true);

            // �q�ɂ̃X�[�p�[���[�U�[�̂݁A�J�[�g���̃f�B�[�v�L�����Z�����ł���
            if (userRole == "super" && txtLoginDept.Text == "PC")
            {
                btnDeepCancelCartonPackTray.Enabled = true;
                btnDeepCancelCartonPackTray.Visible = true;
                btnImportForDeepCancel.Enabled = true;
                btnImportForDeepCancel.Visible = true;

            }
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
            dt.Columns.Add("carton_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("l_cnt", typeof(int));
            dt.Columns.Add("m_qty", typeof(int));
            dt.Columns.Add("batch", typeof(string));
            dt.Columns.Add("register_date", typeof(DateTime));
            dt.Columns.Add("rg_user", typeof(string));
            dt.Columns.Add("cancel_date", typeof(DateTime));
            dt.Columns.Add("cl_user", typeof(string));
            dt.Columns.Add("pallet_id", typeof(string));
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        public void updateDataGridViews(DataTable dt, ref DataGridView dgv, bool load)
        {
            DateTime registerDateFrom = dtpRegsterDateFrom.Value;
            DateTime registerDateTo = dtpRegisterDateTo.Value.AddDays(1);
            string palletId = txtPalletId.Text;
            string cartonId = txtCartonId.Text;
            string lot = txtLot.Text;
            string packId = txtPackId.Text;
            string batch= txtBatch.Text;

            bool b_registerDateFrom = cbxRegisterDateFrom.Checked;
            bool b_registerDateTo = cbxRegisterDateTo.Checked;
            bool b_palletId = cbxPalletId.Checked;
            bool b_cartonId = cbxCartonId.Checked;
            bool b_lot = cbxLot.Checked;
            bool b_packId = cbxPackId.Checked;
            bool b_batch = cbxBatch.Checked;
            bool b_multi_lot = cbxMultiLot.Checked;
            bool b_hideCancel = cbxHideCancel.Checked;

            // ���[�U�[���p�b�N�h�c�����������Ƃ��Ďw�肵���ꍇ�́A�ʂ̂r�p�k�����g�p����
            string sqlX = "select carton_id, lot, l_cnt, m_qty, batch, register_date, rg_user, cancel_date, cl_user, pallet_id from t_carton " +
                          "where carton_id in (select carton_id from t_pack where pack_id like '" + packId + "%')";

            // ���[�U�[���I�����������������A�r�p�k���ɔ��f����
            string sql1 = "select carton_id, lot, l_cnt, m_qty, batch, register_date, rg_user, cancel_date, cl_user, pallet_id from t_carton where ";

            bool[] cr = {                                     true,
                                                              true,
                          palletId == string.Empty ? false : true,
                          cartonId  == string.Empty ? false : true,
                          lot       == string.Empty ? false : true,
                          packId    == string.Empty ? false : true,
                          batch     == string.Empty ? false : true,
                                                              true,
                                                              true};

            bool[] ck = { b_registerDateFrom,
                          b_registerDateTo,
                          b_palletId,
                          b_cartonId,
                          b_lot,
                          b_packId,
                          b_batch,
                          b_multi_lot,
                          b_hideCancel};

            string sql2 = (!(cr[0] && ck[0]) ? string.Empty : "register_date >= '" + registerDateFrom + "' AND ") +
                          (!(cr[1] && ck[1]) ? string.Empty : "register_date < '" + registerDateTo + "' AND ") +
                          (!(cr[2] && ck[2]) ? string.Empty : "pallet_id like '%" + palletId + "%' AND ") +
                          (!(cr[3] && ck[3]) ? string.Empty : "carton_id like '%" + cartonId + "%' AND ") +
                          (!(cr[4] && ck[4]) ? string.Empty : "lot like '%" + lot + "%' AND ") +
                          (!(cr[5] && ck[5]) ? string.Empty : "pack_id like '%" + packId + "%' AND ") +
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

            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 5) + " order by carton_id";
            string sql4 = string.Empty;
            if (packId != string.Empty && b_packId) sql4 = sqlX;
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
            updateDataGridViews(dtCarton, ref dgvCarton, false);
        }

        // �O���b�h�r���[��̃{�^���������A���W���[���t�H�[�����{�����[�h�ŊJ���A�f���Q�[�g����
        private void dgvBoxId_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = int.Parse(e.RowIndex.ToString());

            if (dgvCarton.Columns[e.ColumnIndex] == openCarton && currentRow >= 0)
            {
                //����frmModuleInTray ���J����Ă���ꍇ�́A��������悤����
                if (TfGeneral.checkOpenFormExists("frmPackInCarton"))
                {
                    MessageBox.Show("Please close the currently open form.", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }

                string cartonId = dgvCarton["carton_id", currentRow].Value.ToString();
                DateTime cartonDate = (DateTime)dgvCarton["register_date", currentRow].Value;
                string batch = txtBatch.Text;
                bool canceled = !String.IsNullOrEmpty(dgvCarton["cl_user", currentRow].Value.ToString());
                bool palleted = !String.IsNullOrEmpty(dgvCarton["pallet_id", currentRow].Value.ToString());

                // ���W���[���e�L�X�g�{�b�N�X����łȂ��A���`�F�b�N�{�b�N�X���I���A���������ʂ��P�s�̏ꍇ�̂݁A�ăv�����g���[�h��L��
                bool reprintMode = (txtPackId.Text.Length != 0 && cbxPackId.Checked && dtCarton.Rows.Count == 1);

                frmPackInCarton fP = new frmPackInCarton();
                //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
                fP.RefreshEvent += delegate (object sndr, EventArgs excp)
                {
                    updateDataGridViews(dtCarton, ref dgvCarton, false);
                    this.Focus();
                };

                fP.updateControls(cartonId, cartonDate, userId, txtLoginName.Text, txtLoginDept.Text, userRole, batch, false, false, canceled, palleted, 1, reprintMode);
                fP.Show();
            }
        }

        // frmTrayInPack ��ǉ����[�h�ŊJ���A�f���Q�[�g����
        private void btnAddBoxId_Click(object sender, EventArgs e)
        {
            if (TfGeneral.checkOpenFormExists("frmPackInCarton")) 
            {
                MessageBox.Show("Please close the currently open form.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }

            frmPackInCarton fP = new frmPackInCarton();
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fP.RefreshEvent += delegate(object sndr, EventArgs excp) 
            {
                updateDataGridViews(dtCarton, ref dgvCarton, false);
                this.Focus(); 
            };

            fP.updateControls(String.Empty, DateTime.Now, userId, txtLoginName.Text, txtLoginDept.Text, userRole, string.Empty, true, false, false, false, 1, false);
            fP.Show();
        }

        //frmCarton�����ہA��\���ɂȂ��Ă���e�t�H�[��frmLogin��\������
        private void frmCarton_FormClosed(object sender, FormClosedEventArgs e)
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
            dt = (DataTable)dgvCarton.DataSource;
            ExcelClass xl = new ExcelClass();
            xl.ExportToExcel(dt);
        }

        // �t�H�[��frmTrayInPack���J����Ă��Ȃ����Ƃ��m�F���Ă���A����
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (TfGeneral.checkOpenFormExists("frmPackInTray"))
            {
                MessageBox.Show("Please close the currently open form.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }
            Close();
        }

        // �J�[�g�����x���̈ꊇ�v�����g�A�E�g
        private void btnPrintCarton_Click(object sender, EventArgs e)
        {
            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvCarton.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select only carton id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
            // �񖼂��������ꍇ�̂݁A�������s��
            if (dgvCarton.Columns[dgvCarton.CurrentCell.ColumnIndex].Name != "carton_id")
            {
                MessageBox.Show("Please select only carton id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            // �p���b�g�ς݃J�[�g����I�����Ă���ꍇ�́A������Ȃ�
            foreach (DataGridViewCell cell in dgvCarton.SelectedCells)
            {
                if (dgvCarton["cl_user", cell.RowIndex].Value.ToString() != string.Empty)
                {
                    MessageBox.Show(cell.Value.ToString() + " has been canceled already.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }
            }

            // �I���Z����z��Ɋi�[�A����у��b�Z�[�W�p������𐶐����A�ꊇ�v�����g���s��
            string[] cartonlist = { };
            string message = string.Empty;
            int i = 0;
            foreach (DataGridViewCell cell in dgvCarton.SelectedCells)
            {
                if (dgvCarton["cl_user", cell.RowIndex].Value.ToString() == string.Empty)
                {
                    i += 1;
                    Array.Resize(ref cartonlist, i);
                    cartonlist[i - 1] = cell.Value.ToString();
                    message = message + Environment.NewLine + cell.Value.ToString();
                }
            }
            if (message == string.Empty)
            {
                MessageBox.Show("No carton id was selected.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TfSato tfs = new TfSato();
            for (int j = 0; j < cartonlist.Length; j++)
            {
                //tfs.printStart("cartonPega", cartonlist[j], 0);
            }
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

        // �f�B�[�v�L�����Z���Ώۂ̃J�[�g�����A�f�X�N�g�b�v��CSV���C���|�[�g���đI������
        private void btnImportForDeepCancel_Click(object sender, EventArgs e)
        {
            string sql1 = "select carton_id, lot, l_cnt, m_qty, batch, register_date, rg_user, cancel_date, cl_user, pallet_id from t_carton where carton_id in ( ";
            string sql2 = string.Empty;

            //�N���XTfImport���g�p���ASQL�����쐬����
            List<TfImport> cartonList = TfImport.loadCartonListFromDesktopCsv(
                System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\CartonList.csv");

            foreach (var carton in cartonList)
            {
                sql2 += "'" + carton.CartonNumber + "', ";
            }

            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ") order by carton_id";
            System.Diagnostics.Debug.Print(sql3);

            // �r�p�k���ʂ��A�c�s�`�`�s�`�a�k�d�֊i�[
            dtCarton.Clear();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtCarton);

            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgvCarton.DataSource = dtCarton;

            //�s�w�b�_�[�ɍs�ԍ���\������
            for (int i = 0; i < dgvCarton.Rows.Count; i++) dgvCarton.Rows[i].HeaderCell.Value = (i + 1).ToString();

            //�s�w�b�_�[�̕����������߂���
            dgvCarton.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);

            // ��ԉ��̍s��\������
            if (dgvCarton.Rows.Count != 0) dgvCarton.FirstDisplayedScrollingRowIndex = dgvCarton.Rows.Count - 1;
        }

        // �J�[�g���̃f�B�[�v�L�����Z���i�J�[�g���E�p�b�N�E�g���[�̃L�����Z������o�^���A���W���[�����R�[�h���폜����B
        // �o�b�̃X�[�p�[���[�U�[�Ɍ���������B�p���b�g�Ɋ��ɓo�^����Ă���ꍇ�́A�������s��Ȃ��B
        private void btnDeepCancelCartonPackTray_Click(object sender, EventArgs e)
        {
            if (dgvCarton.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvCarton.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select only carton id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
            // �񖼂��������ꍇ�̂݁A�������s��
            if (dgvCarton.Columns[dgvCarton.CurrentCell.ColumnIndex].Name != "carton_id")
            {
                MessageBox.Show("Please select only carton id column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            // �g���[�ύڍς݃J�[�g�����Ȃ����A�m�F����
            foreach (DataGridViewCell cell in dgvCarton.SelectedCells)
            {
                if (dgvCarton["pallet_id", cell.RowIndex].Value.ToString() != string.Empty ||
                    dgvCarton["cl_user", cell.RowIndex].Value.ToString() != string.Empty)
                {
                    MessageBox.Show(cell.Value.ToString() + " has been already on pallet or canceled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }
            }

            // �{���ɍ폜���Ă悢���A�R�d�Ŋm�F����B
            DialogResult result1 = MessageBox.Show("Do you really want to cancel these carton, pack, tray, and module?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result1 == DialogResult.No) return;
            DialogResult result2 = MessageBox.Show("Are you sure to cancel all the carton, pack, tray, and module?" + Environment.NewLine + "Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result2 == DialogResult.No) return;
            DialogResult result3 = MessageBox.Show("Are you really sure? Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result3 == DialogResult.No) return;

            // �I���Z����z��Ɋi�[�A����у��b�Z�[�W�p������𐶐����A�ꊇ�L�����Z���r�p�k�R�}���h�����s
            string[] cartonlist = { };
            string message = string.Empty;
            int i = 0;
            foreach (DataGridViewCell cell in dgvCarton.SelectedCells)
            {
                if (dgvCarton["cl_user", cell.RowIndex].Value.ToString() == string.Empty)
                {
                    i += 1;
                    Array.Resize(ref cartonlist, i);
                    cartonlist[i - 1] = cell.Value.ToString();
                    message = message + Environment.NewLine + cell.Value.ToString();
                }
            }
            if (message == string.Empty)
            {
                MessageBox.Show("No carton ID was selected.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TfSQL tf = new TfSQL();
            bool res = tf.sqlMultipleDeepCancelCartonPackTray(cartonlist, txtLoginName.Text);

            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                dtCarton.Clear();
                updateDataGridViews(dtCarton, ref dgvCarton, false);
                MessageBox.Show("The following " + i + " carton IDs, their packs, trays, and modules were all canceled: " + message, "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Cancel process was not successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}