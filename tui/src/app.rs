use std::io;

use crossterm::event::{self, Event, KeyCode, KeyEvent, KeyEventKind};
use ratatui::DefaultTerminal;

use crate::ui;

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub enum View {
    #[default]
    Library,
    PostOffice,
}

#[derive(Debug, Default)]
pub struct App {
    pub view: View,
    should_quit: bool,
}

impl App {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn run(mut self, terminal: &mut DefaultTerminal) -> io::Result<()> {
        while !self.should_quit {
            terminal.draw(|frame| ui::render(frame, &self))?;
            self.handle_events()?;
        }

        Ok(())
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
}
