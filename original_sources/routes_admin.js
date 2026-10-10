const express = require('express');
const router = express.Router();
const { protect } = require('../middleware/auth');
const multer = require('multer');
const path = require('path');
const fs = require('fs');
const cloudinary = require('cloudinary').v2;
const Contact = require('../models/Contact');
const Portfolio = require('../models/Portfolio');
const Admin = require('../models/Admin');

// Configure Cloudinary
if (process.env.CLOUDINARY_CLOUD_NAME) {
  cloudinary.config({
    cloud_name: process.env.CLOUDINARY_CLOUD_NAME,
    api_key: process.env.CLOUDINARY_API_KEY,
    api_secret: process.env.CLOUDINARY_API_SECRET
  });
}

// Configure multer for file uploads
const storage = multer.diskStorage({
  destination: function (req, file, cb) {
    const uploadDir = 'uploads/';
    if (!fs.existsSync(uploadDir)) {
      fs.mkdirSync(uploadDir, { recursive: true });
    }
    cb(null, uploadDir);
  },
  filename: function (req, file, cb) {
    const uniqueSuffix = Date.now() + '-' + Math.round(Math.random() * 1E9);
    cb(null, file.fieldname + '-' + uniqueSuffix + path.extname(file.originalname));
  }
});

const upload = multer({
  storage: storage,
  limits: {
    fileSize: 5 * 1024 * 1024 // 5MB limit
  },
  fileFilter: function (req, file, cb) {
    const allowedTypes = /jpeg|jpg|png|gif|webp/;
    const extname = allowedTypes.test(path.extname(file.originalname).toLowerCase());
    const mimetype = allowedTypes.test(file.mimetype);

    if (mimetype && extname) {
      return cb(null, true);
    } else {
      cb(new Error('Only image files are allowed!'));
    }
  }
});

// @desc    Get dashboard statistics
// @route   GET /api/admin/dashboard
// @access  Private
router.get('/dashboard', protect, async (req, res) => {
  try {
    // Get contact statistics
    const contactStats = await Contact.aggregate([
      {
        $group: {
          _id: '$status',
          count: { $sum: 1 }
        }
      }
    ]);

    // Get recent contacts
    const recentContacts = await Contact.find()
      .sort({ createdAt: -1 })
      .limit(5)
      .select('name email message status createdAt');

    // Get portfolio data
    const portfolio = await Portfolio.getPortfolio();

    // Format contact stats
    const stats = {
      total: 0,
      unread: 0,
      read: 0,
      replied: 0
    };

    contactStats.forEach(stat => {
      stats[stat._id] = stat.count;
      stats.total += stat.count;
    });

    // Get recent activity (last 7 days)
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const recentActivity = await Contact.countDocuments({
      createdAt: { $gte: sevenDaysAgo }
    });

    res.json({
      success: true,
      data: {
        contactStats: stats,
        recentContacts,
        portfolio: {
          projectsCount: portfolio.projects?.items?.length || 0,
          skillsCount: portfolio.about?.skills?.length || 0,
          lastUpdated: portfolio.updatedAt
        },
        recentActivity
      }
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Server Error'
    });
  }
});

// @desc    Upload image
// @route   POST /api/admin/upload
// @access  Private
router.post('/upload', protect, upload.single('image'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({
        success: false,
        error: 'No file uploaded'
      });
    }

    let imageUrl = '';

    // Upload to Cloudinary if configured
    if (process.env.CLOUDINARY_CLOUD_NAME) {
      try {
        const result = await cloudinary.uploader.upload(req.file.path, {
          folder: 'portfolio',
          use_filename: true
        });
        imageUrl = result.secure_url;

        // Delete local file after cloudinary upload
        fs.unlinkSync(req.file.path);
      } catch (cloudinaryError) {
        console.error('Cloudinary upload error:', cloudinaryError);
        // Fallback to local file
        imageUrl = `${req.protocol}://${req.get('host')}/uploads/${req.file.filename}`;
      }
    } else {
      // Use local file
      imageUrl = `${req.protocol}://${req.get('host')}/uploads/${req.file.filename}`;
    }

    res.json({
      success: true,
      data: {
        url: imageUrl,
        filename: req.file.filename,
        originalName: req.file.originalname,
        size: req.file.size
      }
    });
  } catch (error) {
    // Clean up file if error occurs
    if (req.file && fs.existsSync(req.file.path)) {
      fs.unlinkSync(req.file.path);
    }

    res.status(500).json({
      success: false,
      error: error.message
    });
  }
});

// @desc    Delete uploaded file
// @route   DELETE /api/admin/upload/:filename
// @access  Private
router.delete('/upload/:filename', protect, async (req, res) => {
  try {
    const { filename } = req.params;
    const filePath = path.join('uploads', filename);

    if (fs.existsSync(filePath)) {
      fs.unlinkSync(filePath);
    }

    res.json({
      success: true,
      message: 'File deleted successfully'
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to delete file'
    });
  }
});

// @desc    Get all uploaded files
// @route   GET /api/admin/uploads
// @access  Private
router.get('/uploads', protect, async (req, res) => {
  try {
    const uploadDir = 'uploads/';
    const files = [];

    if (fs.existsSync(uploadDir)) {
      const fileList = fs.readdirSync(uploadDir);
      
      for (const file of fileList) {
        const filePath = path.join(uploadDir, file);
        const stats = fs.statSync(filePath);
        
        files.push({
          name: file,
          size: stats.size,
          created: stats.birthtime,
          url: `${req.protocol}://${req.get('host')}/uploads/${file}`
        });
      }
    }

    res.json({
      success: true,
      data: files
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to get files'
    });
  }
});

// @desc    Export portfolio data
// @route   GET /api/admin/export
// @access  Private
router.get('/export', protect, async (req, res) => {
  try {
    const portfolio = await Portfolio.getPortfolio();
    const contacts = await Contact.find().sort({ createdAt: -1 });

    const exportData = {
      portfolio,
      contacts,
      exportedAt: new Date().toISOString(),
      totalContacts: contacts.length
    };

    res.setHeader('Content-Type', 'application/json');
    res.setHeader('Content-Disposition', `attachment; filename=portfolio-export-${Date.now()}.json`);
    
    res.json(exportData);
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to export data'
    });
  }
});

// @desc    Import portfolio data
// @route   POST /api/admin/import
// @access  Private
router.post('/import', protect, async (req, res) => {
  try {
    const { portfolio, contacts } = req.body;

    if (portfolio) {
      await Portfolio.findOneAndUpdate({}, portfolio, { upsert: true, new: true });
    }

    if (contacts && Array.isArray(contacts)) {
      // Clear existing contacts and import new ones
      await Contact.deleteMany({});
      if (contacts.length > 0) {
        await Contact.insertMany(contacts);
      }
    }

    res.json({
      success: true,
      message: 'Data imported successfully'
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});

// @desc    Backup database
// @route   POST /api/admin/backup
// @access  Private
router.post('/backup', protect, async (req, res) => {
  try {
    const portfolio = await Portfolio.getPortfolio();
    const contacts = await Contact.find().sort({ createdAt: -1 });
    const admins = await Admin.find().select('-password');

    const backup = {
      portfolio,
      contacts,
      admins,
      backupDate: new Date().toISOString(),
      version: '1.0'
    };

    // Save backup to file
    const backupDir = 'backups/';
    if (!fs.existsSync(backupDir)) {
      fs.mkdirSync(backupDir, { recursive: true });
    }

    const backupFile = path.join(backupDir, `backup-${Date.now()}.json`);
    fs.writeFileSync(backupFile, JSON.stringify(backup, null, 2));

    res.json({
      success: true,
      message: 'Backup created successfully',
      data: {
        filename: path.basename(backupFile),
        size: fs.statSync(backupFile).size,
        date: backup.backupDate
      }
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to create backup'
    });
  }
});

// @desc    Update hero section
// @route   PUT /api/admin/portfolio/hero
// @access  Private
router.put('/portfolio/hero', protect, async (req, res) => {
  try {
    const { title, subtitle, description, cvUrl, socialLinks } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    portfolio.hero = {
      title,
      subtitle,
      description,
      cvUrl,
      socialLinks
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Hero section updated successfully',
      data: portfolio.hero
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update hero section'
    });
  }
});

// @desc    Update about section
// @route   PUT /api/admin/portfolio/about
// @access  Private
router.put('/portfolio/about', protect, async (req, res) => {
  try {
    const { title, subtitle, description, paragraph1, paragraph2, paragraph3, profileImage, skills } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    portfolio.about = {
      ...portfolio.about,
      title,
      subtitle,
      description,
      paragraph1,
      paragraph2,
      paragraph3,
      profileImage,
      skills
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'About section updated successfully',
      data: portfolio.about
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update about section'
    });
  }
});

// @desc    Update contact section
// @route   PUT /api/admin/portfolio/contact
// @access  Private
router.put('/portfolio/contact', protect, async (req, res) => {
  try {
    const { title, subtitle, description, responseTime } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    portfolio.contact = {
      ...portfolio.contact,
      title,
      subtitle,
      description,
      responseTime
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Contact section updated successfully',
      data: portfolio.contact
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update contact section'
    });
  }
});

// @desc    Add new contact info
// @route   POST /api/admin/portfolio/contact-info
// @access  Private
router.post('/portfolio/contact-info', protect, async (req, res) => {
  try {
    const { icon, title, value, href } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    
    const newContactInfo = {
      icon,
      title,
      value,
      href: href || '#'
    };
    
    if (!portfolio.contact) {
      portfolio.contact = {};
    }
    
    if (!portfolio.contact.contactInfo) {
      portfolio.contact.contactInfo = [];
    }
    
    portfolio.contact.contactInfo.push(newContactInfo);
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Contact info added successfully',
      data: portfolio.contact.contactInfo
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to add contact info'
    });
  }
});

// @desc    Update contact info
// @route   PUT /api/admin/portfolio/contact-info/:index
// @access  Private
router.put('/portfolio/contact-info/:index', protect, async (req, res) => {
  try {
    const { icon, title, value, href } = req.body;
    const index = parseInt(req.params.index);
    
    const portfolio = await Portfolio.getPortfolio();
    
    if (!portfolio.contact || !portfolio.contact.contactInfo || index < 0 || index >= portfolio.contact.contactInfo.length) {
      return res.status(404).json({
        success: false,
        error: 'Contact info not found'
      });
    }
    
    portfolio.contact.contactInfo[index] = {
      icon,
      title,
      value,
      href: href || '#'
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Contact info updated successfully',
      data: portfolio.contact.contactInfo
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update contact info'
    });
  }
});

// @desc    Delete contact info
// @route   DELETE /api/admin/portfolio/contact-info/:index
// @access  Private
router.delete('/portfolio/contact-info/:index', protect, async (req, res) => {
  try {
    const index = parseInt(req.params.index);
    
    const portfolio = await Portfolio.getPortfolio();
    
    if (!portfolio.contact || !portfolio.contact.contactInfo || index < 0 || index >= portfolio.contact.contactInfo.length) {
      return res.status(404).json({
        success: false,
        error: 'Contact info not found'
      });
    }
    
    portfolio.contact.contactInfo.splice(index, 1);
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Contact info deleted successfully',
      data: portfolio.contact.contactInfo
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to delete contact info'
    });
  }
});

// @desc    Update footer section
// @route   PUT /api/admin/portfolio/footer
// @access  Private
router.put('/portfolio/footer', protect, async (req, res) => {
  try {
    const { copyright, description, additionalLinks } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    portfolio.footer = {
      ...portfolio.footer,
      copyright,
      description,
      additionalLinks: additionalLinks || []
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Footer section updated successfully',
      data: portfolio.footer
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update footer section'
    });
  }
});

// @desc    Add new project
// @route   POST /api/admin/portfolio/projects
// @access  Private
router.post('/portfolio/projects', protect, async (req, res) => {
  try {
    const { title, description, technologies, liveUrl, frontendUrl, backendUrl, image } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    
    const newProject = {
      title,
      description,
      technologies: Array.isArray(technologies) ? technologies : technologies.split(',').map(t => t.trim()),
      liveUrl,
      frontendUrl,
      backendUrl,
      image,
      featured: false,
      order: portfolio.projects?.items?.length || 0
    };
    
    if (!portfolio.projects) {
      portfolio.projects = { items: [] };
    }
    
    portfolio.projects.items.push(newProject);
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Project added successfully',
      data: newProject
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to add project'
    });
  }
});

// @desc    Update project
// @route   PUT /api/admin/portfolio/projects/:id
// @access  Private
router.put('/portfolio/projects/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    const updateData = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    const projectIndex = portfolio.projects?.items?.findIndex(p => p._id.toString() === id);
    
    if (projectIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Project not found'
      });
    }
    
    portfolio.projects.items[projectIndex] = {
      ...portfolio.projects.items[projectIndex],
      ...updateData
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Project updated successfully',
      data: portfolio.projects.items[projectIndex]
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update project'
    });
  }
});

// @desc    Delete project
// @route   DELETE /api/admin/portfolio/projects/:id
// @access  Private
router.delete('/portfolio/projects/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    
    const portfolio = await Portfolio.getPortfolio();
    const projectIndex = portfolio.projects?.items?.findIndex(p => p._id.toString() === id);
    
    if (projectIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Project not found'
      });
    }
    
    portfolio.projects.items.splice(projectIndex, 1);
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Project deleted successfully'
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to delete project'
    });
  }
});

// @desc    Add new skill
// @route   POST /api/admin/portfolio/skills
// @access  Private
router.post('/portfolio/skills', protect, async (req, res) => {
  try {
    const { icon, title, description } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    
    const newSkill = {
      icon,
      title,
      description
    };
    
    if (!portfolio.about) {
      portfolio.about = { skills: [] };
    }
    if (!portfolio.about.skills) {
      portfolio.about.skills = [];
    }
    
    portfolio.about.skills.push(newSkill);
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Skill added successfully',
      data: newSkill
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to add skill'
    });
  }
});

// @desc    Update skill
// @route   PUT /api/admin/portfolio/skills/:id
// @access  Private
router.put('/portfolio/skills/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    const { icon, title, description } = req.body;
    
    const portfolio = await Portfolio.getPortfolio();
    const skillIndex = portfolio.about?.skills?.findIndex(s => s._id.toString() === id);
    
    if (skillIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Skill not found'
      });
    }
    
    portfolio.about.skills[skillIndex] = {
      ...portfolio.about.skills[skillIndex],
      icon,
      title,
      description
    };
    
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Skill updated successfully',
      data: portfolio.about.skills[skillIndex]
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update skill'
    });
  }
});

// @desc    Delete skill
// @route   DELETE /api/admin/portfolio/skills/:id
// @access  Private
router.delete('/portfolio/skills/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    
    const portfolio = await Portfolio.getPortfolio();
    const skillIndex = portfolio.about?.skills?.findIndex(s => s._id.toString() === id);
    
    if (skillIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Skill not found'
      });
    }
    
    portfolio.about.skills.splice(skillIndex, 1);
    await portfolio.save();
    
    res.json({
      success: true,
      message: 'Skill deleted successfully'
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to delete skill'
    });
  }
});

// @desc    Get all admins
// @route   GET /api/admin/admins
// @access  Private
router.get('/admins', protect, async (req, res) => {
  try {
    const admins = await Admin.find().select('-password').sort({ createdAt: -1 });
    
    res.json({
      success: true,
      data: admins
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to fetch admins'
    });
  }
});

// @desc    Create new admin
// @route   POST /api/admin/admins
// @access  Private
router.post('/admins', protect, async (req, res) => {
  try {
    const { name, email, password, role } = req.body;
    
    // Check if admin already exists
    const existingAdmin = await Admin.findOne({ email });
    if (existingAdmin) {
      return res.status(400).json({
        success: false,
        error: 'Admin with this email already exists'
      });
    }
    
    const admin = new Admin({
      name,
      email,
      password,
      role: role || 'admin'
    });
    
    await admin.save();
    
    const adminData = admin.toObject();
    delete adminData.password;
    
    res.json({
      success: true,
      message: 'Admin created successfully',
      data: adminData
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to create admin'
    });
  }
});

// @desc    Update admin
// @route   PUT /api/admin/admins/:id
// @access  Private
router.put('/admins/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    const updateData = req.body;
    
    if (updateData.password) {
      updateData.password = await Admin.hashPassword(updateData.password);
    }
    
    const admin = await Admin.findByIdAndUpdate(id, updateData, { new: true }).select('-password');
    
    if (!admin) {
      return res.status(404).json({
        success: false,
        error: 'Admin not found'
      });
    }
    
    res.json({
      success: true,
      message: 'Admin updated successfully',
      data: admin
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to update admin'
    });
  }
});

// @desc    Delete admin
// @route   DELETE /api/admin/admins/:id
// @access  Private
router.delete('/admins/:id', protect, async (req, res) => {
  try {
    const { id } = req.params;
    
    const admin = await Admin.findByIdAndDelete(id);
    
    if (!admin) {
      return res.status(404).json({
        success: false,
        error: 'Admin not found'
      });
    }
    
    res.json({
      success: true,
      message: 'Admin deleted successfully'
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Failed to delete admin'
    });
  }
});

module.exports = router; 