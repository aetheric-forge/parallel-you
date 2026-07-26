use ratatui::{
    Frame,
    layout::{Alignment, Constraint, Direction, Layout},
    widgets::{Block, Borders, Paragraph},
};

use crate::app::App;

pub fn render(frame: &mut Frame, _app: &App) {
    let areas = Layout::default()
        .direction(Direction::Vertical)
        .constraints([
            Constraint::Length(3),
            Constraint::Min(1),
            Constraint::Length(3),
        ])
        .split(frame.area());

    let header = Paragraph::new("Parallel You")
        .alignment(Alignment::Center)
        .block(
            Block::default()
                .title(" Aetheric Forge ")
                .borders(Borders::ALL),
        );

    let workspace = Paragraph::new(
        "Abstract application shell\n\n\
         Archives · Staging · Library · Post Office",
    )
    .alignment(Alignment::Center)
    .block(Block::default().title(" Workspace ").borders(Borders::ALL));

    let footer = Paragraph::new("q / Esc: quit")
        .alignment(Alignment::Center)
        .block(Block::default().borders(Borders::ALL));

    frame.render_widget(header, areas[0]);
    frame.render_widget(workspace, areas[1]);
    frame.render_widget(footer, areas[2]);
}
