const path = require('path');
require('dotenv').config({ path: path.resolve(__dirname, '.env') });
const express = require('express');
const cors    = require('cors');
const { Pool } = require('pg');

const app  = express();
const pool = new Pool({
  host:     process.env.DB_HOST     || 'localhost',
  port:     Number(process.env.DB_PORT) || 5433,
  database: process.env.DB_NAME     || 'postgres',
  user:     process.env.DB_USER     || 'postgres',
  password: process.env.DB_PASSWORD || '123456'
});

app.use(cors());
app.use(express.json());

app.get('/api/news', async (req, res) => {
  try {
    const { rows } = await pool.query('SELECT * FROM news ORDER BY sort_order ASC');
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Database error' });
  }
});

app.post('/api/news', async (req, res) => {
  const { id, img, date_en, date_ar, title_en, title_ar, blocks_en, blocks_ar } = req.body;
  try {
    await pool.query(
      `INSERT INTO news (id, img, date_en, date_ar, title_en, title_ar, blocks_en, blocks_ar, sort_order)
       VALUES ($1,$2,$3,$4,$5,$6,$7,$8, (SELECT COALESCE(MAX(sort_order),0)+1 FROM news))`,
      [id, img, date_en, date_ar, title_en, title_ar, JSON.stringify(blocks_en), JSON.stringify(blocks_ar)]
    );
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

app.get('/api/articles', async (req, res) => {
  try {
    const { rows } = await pool.query('SELECT * FROM articles ORDER BY sort_order ASC');
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Database error' });
  }
});

app.post('/api/articles', async (req, res) => {
  const { id, img, date_en, date_ar, title_en, title_ar, blocks_en, blocks_ar } = req.body;
  try {
    await pool.query(
      `INSERT INTO articles (id, img, date_en, date_ar, title_en, title_ar, blocks_en, blocks_ar, sort_order)
       VALUES ($1,$2,$3,$4,$5,$6,$7,$8, (SELECT COALESCE(MAX(sort_order),0)+1 FROM articles))`,
      [id, img, date_en, date_ar, title_en, title_ar, JSON.stringify(blocks_en), JSON.stringify(blocks_ar)]
    );
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Backend running on http://localhost:${PORT}`));
