const mongoose = require('mongoose');

const skillSchema = new mongoose.Schema({
  icon: {
    type: String,
    required: true
  },
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    required: true
  }
});

const projectSchema = new mongoose.Schema({
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    required: true
  },
  technologies: [{
    type: String,
    required: true
  }],
  frontendUrl: {
    type: String,
    default: ''
  },
  backendUrl: {
    type: String,
    default: ''
  },
  mobileUrl: {
    type: String,
    default: ''
  },
  liveUrl: {
    type: String,
    default: ''
  },
  image: {
    type: String,
    default: ''
  },
  featured: {
    type: Boolean,
    default: false
  },
  order: {
    type: Number,
    default: 0
  }
});

const contactInfoSchema = new mongoose.Schema({
  icon: {
    type: String,
    required: true
  },
  title: {
    type: String,
    required: true
  },
  value: {
    type: String,
    required: true
  },
  href: {
    type: String,
    default: ''
  }
});

const portfolioSchema = new mongoose.Schema({
  // Hero Section
  hero: {
    title: {
      type: String,
      required: true,
      default: 'Full Stack Developer'
    },
    subtitle: {
      type: String,
      required: true,
      default: 'Building modern web applications with React, Node.js, and MongoDB'
    },
    description: {
      type: String,
      required: true,
      default: 'Passionate about creating clean, efficient code and beautiful user experiences. Let\'s build something amazing together.'
    },
    cvUrl: {
      type: String,
      default: ''
    },
    socialLinks: {
      github: {
        type: String,
        default: 'https://github.com/abdojat'
      },
      linkedin: {
        type: String,
        default: 'https://www.linkedin.com/in/abdulmajid-alawad-0286ab198/'
      },
      email: {
        type: String,
        default: 'abdalaoad2003@hotmail.com'
      }
    }
  },

  // About Section
  about: {
    title: {
      type: String,
      required: true,
      default: 'About Me'
    },
    subtitle: {
      type: String,
      required: true,
      default: 'A passionate developer with expertise in modern web technologies'
    },
    description: {
      type: String,
      required: true,
      default: 'I\'m a dedicated full-stack developer with a passion for creating innovative web applications. With extensive experience in the MERN stack, I specialize in building scalable, user-friendly solutions that solve real-world problems.'
    },
    paragraph1: {
      type: String,
      default: 'My journey in web development started with a curiosity about how websites work, and it has evolved into a career focused on clean code, optimal performance, and exceptional user experiences.'
    },
    paragraph2: {
      type: String,
      default: 'When I\'m not coding, you can find me exploring new technologies, contributing to open-source projects, or sharing knowledge with the developer community.'
    },
    paragraph3: {
      type: String,
      default: ''
    },
    profileImage: {
      type: String,
      default: 'https://res.cloudinary.com/dndyr4vlq/image/upload/v1752930937/ecommerce/pbrsngxpo58wfp3efxdk.jpg'
    },
    skills: [skillSchema]
  },

  // Projects Section
  projects: {
    title: {
      type: String,
      required: true,
      default: 'Featured Projects'
    },
    subtitle: {
      type: String,
      required: true,
      default: 'A showcase of my recent work and personal projects'
    },
    items: [projectSchema]
  },

  // Contact Section
  contact: {
    title: {
      type: String,
      required: true,
      default: 'Get In Touch'
    },
    subtitle: {
      type: String,
      required: true,
      default: 'Let\'s discuss your next project or collaboration opportunity'
    },
    description: {
      type: String,
      required: true,
      default: 'I\'m always interested in hearing about new opportunities and exciting projects. Whether you\'re a company looking to hire, or you\'re a fellow developer wanting to collaborate, I\'d love to hear from you.'
    },
    contactInfo: [contactInfoSchema],
    responseTime: {
      type: String,
      default: 'Typically responds within 24 hours'
    }
  },

  // Footer
  footer: {
    copyright: {
      type: String,
      default: '© 2024 Abdulmajid Alawad. All rights reserved.'
    },
    description: {
      type: String,
      default: 'Building digital experiences with passion and precision.'
    },
    additionalLinks: [{
      text: String,
      url: String
    }]
  }
}, {
  timestamps: true
});

// Create a single portfolio document
portfolioSchema.statics.getPortfolio = async function() {
  let portfolio = await this.findOne();
  
  if (!portfolio) {
    // Create default portfolio data
    portfolio = await this.create({
      about: {
        paragraph1: 'My journey in web development started with a curiosity about how websites work, and it has evolved into a career focused on clean code, optimal performance, and exceptional user experiences.',
        paragraph2: 'When I\'m not coding, you can find me exploring new technologies, contributing to open-source projects, or sharing knowledge with the developer community.',
        paragraph3: '',
        skills: [
          {
            icon: 'Code',
            title: 'Frontend Development',
            description: 'React, TypeScript, Tailwind CSS, Next.js'
          },
          {
            icon: 'Database',
            title: 'Backend Development',
            description: 'Node.js, Express, MongoDB, PostgreSQL'
          },
          {
            icon: 'Globe',
            title: 'Full Stack Applications',
            description: 'MERN stack, RESTful APIs, Authentication'
          },
          {
            icon: 'Smartphone',
            title: 'Mobile Responsive',
            description: 'Progressive Web Apps, Mobile-first design'
          }
        ]
      },
      contact: {
        contactInfo: [
          {
            icon: 'Mail',
            title: 'Email',
            value: 'abdalaoad2003@hotmail.com',
            href: 'mailto:abdalaoad2003@hotmail.com'
          },
          {
            icon: 'Phone',
            title: 'Phone',
            value: '+963940626811',
            href: 'tel:+963940626811'
          },
          {
            icon: 'MapPin',
            title: 'Location',
            value: 'Damascus, Syria',
            href: '#'
          }
        ]
      },
      footer: {
        copyright: '© 2024 Abdulmajid Alawad. All rights reserved.',
        description: 'Building digital experiences with passion and precision.',
        additionalLinks: []
      }
    });
  }
  
  // Ensure footer description field exists for existing portfolios
  if (portfolio && (!portfolio.footer || !portfolio.footer.description)) {
    if (!portfolio.footer) {
      portfolio.footer = {};
    }
    portfolio.footer.description = portfolio.footer.description || 'Building digital experiences with passion and precision.';
    await portfolio.save();
  }
  
  return portfolio;
};

module.exports = mongoose.model('Portfolio', portfolioSchema); 