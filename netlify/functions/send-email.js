const nodemailer = require('nodemailer');

exports.handler = async (event) => {
  if (event.httpMethod !== 'POST') {
    return { statusCode: 405, body: 'Method Not Allowed' };
  }

  let body;
  try {
    body = JSON.parse(event.body);
  } catch {
    return { statusCode: 400, body: JSON.stringify({ error: 'Invalid JSON' }) };
  }

  const { name, email, company, message } = body;

  if (!name || !email || !message) {
    return { statusCode: 400, body: JSON.stringify({ error: 'Missing required fields' }) };
  }

  const transporter = nodemailer.createTransport({
    host: 'smtp.gmail.com',
    port: 587,
    secure: false,
    auth: {
      user: process.env.SMTP_USER || 'CreditPlus.Travo@Gmail.com',
      pass: process.env.SMTP_PASS || 'rxvkpiraffkzusll'
    }
  });

  try {
    await transporter.sendMail({
      from: `"Credit Plus Contact" <CreditPlus.Travo@Gmail.com>`,
      to: 'support@credit-plus.me',
      replyTo: email,
      subject: `New Contact from ${name} — Credit Plus`,
      html: `
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
          <h2 style="color:#15206b">New Contact Request</h2>
          <table style="width:100%;border-collapse:collapse">
            <tr><td style="padding:8px 0;color:#666;width:120px"><b>Name</b></td><td style="padding:8px 0">${name}</td></tr>
            <tr><td style="padding:8px 0;color:#666"><b>Email</b></td><td style="padding:8px 0"><a href="mailto:${email}">${email}</a></td></tr>
            <tr><td style="padding:8px 0;color:#666"><b>Company</b></td><td style="padding:8px 0">${company || '—'}</td></tr>
          </table>
          <hr style="margin:16px 0;border:none;border-top:1px solid #eee">
          <h3 style="color:#15206b">Message</h3>
          <p style="color:#333;line-height:1.7;white-space:pre-wrap">${message}</p>
          <hr style="margin:16px 0;border:none;border-top:1px solid #eee">
          <p style="color:#aaa;font-size:12px">Sent from credit-plus.me contact form</p>
        </div>
      `
    });

    return {
      statusCode: 200,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ok: true })
    };
  } catch (err) {
    console.error('Email send error:', err);
    return {
      statusCode: 500,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ error: err.message })
    };
  }
};
