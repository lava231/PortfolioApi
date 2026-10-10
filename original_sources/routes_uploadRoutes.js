// routes/uploadRoutes.js


const express = require('express');
const { parser } = require('../config/cloudinary');
const router = express.Router();

router.post('/image', parser.single('image'), (req, res) => {
    console.log(req.file);
    if (!req.file) return res.status(400).json({ error: 'No file uploaded' });
    res.json({ imageUrl: req.file.path });
});

module.exports = router;
