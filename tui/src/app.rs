use std::io;

use crossterm::event::{self, Event, KeyCode, KeyEvent, KeyEventKind};
use ratatui::DefaultTerminal;

use crate::{
    providers::{library::LibraryProvider, post_office::PostOfficeProvider},
    ui,
};

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub enum View {
    #[default]
    Library,
    PostOffice,
}

pub struct App {
    pub view: View,
    should_quit: bool,
    library: Box<dyn LibraryProvider>,
    post_office: Box<dyn PostOfficeProvider>,
    library_status: String,
    post_office_status: String,
}

impl App {
    pub fn new(
        library: impl LibraryProvider + 'static,
        post_office: impl PostOfficeProvider + 'static,
    ) -> Self {
        let mut app = Self {
            view: View::default(),
            should_quit: false,
            library: Box::new(library),
            post_office: Box::new(post_office),
            library_status: String::new(),
            post_office_status: String::new(),
        };

        app.refresh_provider_status();
        app
    }

    pub fn run(mut self, terminal: &mut DefaultTerminal) -> io::Result<()> {
        while !self.should_quit {
            terminal.draw(|frame| ui::render(frame, &self))?;
            self.handle_events()?;
        }

        Ok(())
    }

    pub fn library_name(&self) -> &'static str {
        self.library.name()
    }

    pub fn library_status(&self) -> &str {
        &self.library_status
    }

    pub fn post_office_name(&self) -> &'static str {
        self.post_office.name()
    }

    pub fn post_office_status(&self) -> &str {
        &self.post_office_status
    }

    fn handle_events(&mut self) -> io::Result<()> {
        match event::read()? {
            Event::Key(key) if key.kind == KeyEventKind::Press => {
                self.handle_key_event(key);
            }
            Event::Resize(_, _) => {}
            _ => {}
        }

        Ok(())
    }

    fn handle_key_event(&mut self, key: KeyEvent) {
        match key.code {
            KeyCode::Char('1') => self.view = View::Library,
            KeyCode::Char('2') => self.view = View::PostOffice,
            KeyCode::Tab => self.toggle_view(),
            KeyCode::Char('r') => self.refresh_provider_status(),
            KeyCode::Char('q') | KeyCode::Esc => self.should_quit = true,
            _ => {}
        }
    }

    fn toggle_view(&mut self) {
        self.view = match self.view {
            View::Library => View::PostOffice,
            View::PostOffice => View::Library,
        };
    }

    fn refresh_provider_status(&mut self) {
        self.library_status = match self.library.health_check() {
            Ok(()) => "Available".to_string(),
            Err(error) => format!("Unavailable: {error}"),
        };

        self.post_office_status = match self.post_office.health_check() {
            Ok(()) => "Available".to_string(),
            Err(error) => format!("Unavailable: {error}"),
        };
    }
}
