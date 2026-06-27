-- Create tables
CREATE TABLE IF NOT EXISTS news (
  id         VARCHAR(50)  PRIMARY KEY,
  img        VARCHAR(255),
  date_en    VARCHAR(100),
  date_ar    VARCHAR(100),
  title_en   TEXT,
  title_ar   TEXT,
  blocks_en  JSONB,
  blocks_ar  JSONB,
  sort_order INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS articles (
  id         VARCHAR(50)  PRIMARY KEY,
  img        VARCHAR(255),
  date_en    VARCHAR(100),
  date_ar    VARCHAR(100),
  title_en   TEXT,
  title_ar   TEXT,
  blocks_en  JSONB,
  blocks_ar  JSONB,
  sort_order INT DEFAULT 0
);

-- Add blocks columns if tables already exist
ALTER TABLE news     ADD COLUMN IF NOT EXISTS blocks_en JSONB;
ALTER TABLE news     ADD COLUMN IF NOT EXISTS blocks_ar JSONB;
ALTER TABLE articles ADD COLUMN IF NOT EXISTS blocks_en JSONB;
ALTER TABLE articles ADD COLUMN IF NOT EXISTS blocks_ar JSONB;

-- Seed news
INSERT INTO news (id, img, date_en, date_ar, title_en, title_ar, sort_order) VALUES
  ('hb',      'news-hb.jpg',      'July 26, 2025',   '26 تموز 2025',   'Proud to Power Housing Bank''s New Supply Chain Finance Program',                'نفخر بتشغيل برنامج تمويل سلسلة التوريد الجديد لبنك الإسكان', 1),
  ('poc',     'news-poc.jpg',     '2025',            '2025',           'JOPACC & Credit Plus Complete a Successful Proof of Concept with Housing Bank', 'جوباك وكريدت بلس تكملان إثبات مفهوم ناجح مع بنك الإسكان',   2),
  ('seminar', 'news-seminar.jpg', 'April 26, 2026',  '26 نيسان 2026',  'Supply Chain Finance Session with the Association of Banks in Jordan',          'جلسة تمويل سلسلة التوريد مع جمعية البنوك في الأردن',         3)
ON CONFLICT (id) DO NOTHING;

-- Seed articles
INSERT INTO articles (id, img, date_en, date_ar, title_en, title_ar, sort_order) VALUES
  ('buyers-scf', 'art-buyers.jpg', 'Insight', 'رؤية', 'Why Corporate Buyers Use Supply Chain Finance Platforms - It''s Time to Eliminate Post-Dated Cheques', 'لماذا يستخدم المشترون من الشركات منصات تمويل سلسلة التوريد؟ حان وقت التخلص من الشيكات المؤجلة', 1),
  ('ccc',        'art-ccc.jpg',    'Insight', 'رؤية', 'What Is the Cash Conversion Cycle (CCC)?',                                                               'ما هي دورة تحويل النقد (CCC)؟',                                                                   2),
  ('pwc',        'art-pwc.jpg',    'Insight', 'رؤية', 'Understanding Supply Chain Finance - by PwC',                                                            'فهم تمويل سلسلة التوريد - عن PwC',                                                                3)
ON CONFLICT (id) DO NOTHING;
