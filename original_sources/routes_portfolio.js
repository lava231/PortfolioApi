const express = require('express');
const router = express.Router();
const { protect } = require('../middleware/auth');
const Portfolio = require('../models/Portfolio');

// @desc    Get portfolio data
// @route   GET /api/portfolio
// @access  Public
router.get('/', async (req, res) => {
  try {
    const portfolio = await Portfolio.getPortfolio();
    res.json({
      success: true,
      data: portfolio
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Server Error'
    });
  }
});

// @desc    Update portfolio data
// @route   PUT /api/portfolio
// @access  Private
router.put('/', protect, async (req, res) => {
  try {
    let portfolio = await Portfolio.findOne();
    
    if (!portfolio) {
      portfolio = await Portfolio.create(req.body);
    } else {
      portfolio = await Portfolio.findOneAndUpdate(
        {},
        req.body,
        { new: true, runValidators: true }
      );
    }

    res.json({
      success: true,
      data: portfolio
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});


// @desc    Add project
// @route   POST /api/portfolio/projects
// @access  Private
router.post('/projects', protect, async (req, res) => {
  try {
    let portfolio = await Portfolio.findOne();
    
    if (!portfolio) {
      portfolio = await Portfolio.create({
        projects: { items: [req.body] }
      });
    } else {
      portfolio.projects.items.push(req.body);
      await portfolio.save();
    }

    res.json({
      success: true,
      data: portfolio.projects.items[portfolio.projects.items.length - 1]
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});

// @desc    Update project
// @route   PUT /api/portfolio/projects/:id
// @access  Private
router.put('/projects/:id', protect, async (req, res) => {
  try {
    const portfolio = await Portfolio.findOne();
    
    if (!portfolio) {
      return res.status(404).json({
        success: false,
        error: 'Portfolio not found'
      });
    }

    const projectIndex = portfolio.projects.items.findIndex(
      project => project._id.toString() === req.params.id
    );

    if (projectIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Project not found'
      });
    }

    portfolio.projects.items[projectIndex] = {
      ...portfolio.projects.items[projectIndex].toObject(),
      ...req.body
    };

    await portfolio.save();

    res.json({
      success: true,
      data: portfolio.projects.items[projectIndex]
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});

// @desc    Delete project
// @route   DELETE /api/portfolio/projects/:id
// @access  Private
router.delete('/projects/:id', protect, async (req, res) => {
  try {
    const portfolio = await Portfolio.findOne();
    
    if (!portfolio) {
      return res.status(404).json({
        success: false,
        error: 'Portfolio not found'
      });
    }

    portfolio.projects.items = portfolio.projects.items.filter(
      project => project._id.toString() !== req.params.id
    );

    await portfolio.save();

    res.json({
      success: true,
      message: 'Project deleted successfully'
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});


// @desc    Update specific section
// @route   PUT /api/portfolio/:section
// @access  Private
router.put('/:section', protect, async (req, res) => {
  try {
    const { section } = req.params;
    const updateData = { [section]: req.body };

    let portfolio = await Portfolio.findOne();
    
    if (!portfolio) {
      portfolio = await Portfolio.create(updateData);
    } else {
      portfolio = await Portfolio.findOneAndUpdate(
        {},
        updateData,
        { new: true, runValidators: true }
      );
    }

    res.json({
      success: true,
      data: portfolio[section]
    });
  } catch (error) {
    res.status(400).json({
      success: false,
      error: error.message
    });
  }
});

module.exports = router; 